using AwesomeAssertions;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Parsing;
using Mavlink.Protocol.Generator.Tests.Harness;
using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator.Tests.Linking;

/// <summary>
/// Reading dialects from referenced assemblies. The assemblies are compiled in memory from
/// hand-written code shaped like the generator's output (manifest, dialect class, enums).
/// </summary>
public sealed class ReferencedDialectReaderTests
{
	private const string MinimalSource = """
		[assembly: global::Mavlink.Dialects.Infrastructure.MavlinkDialectManifest(
			1, "minimal", "Mavlink.Minimal", typeof(global::Mavlink.Dialects.MinimalDialect),
			Includes = new string[0], XmlHash = "abc")]

		namespace Mavlink.Dialects
		{
			public sealed class MinimalDialect { }
		}

		namespace Mavlink.Minimal
		{
			[global::Mavlink.Dialects.Infrastructure.MavlinkEnum("MAV_MODE_FLAG")]
			[global::System.Flags]
			public enum MavModeFlag : byte { CustomModeEnabled = 1, SafetyArmed = 128 }

			public readonly struct MavModeFlagBitmask { }

			[global::Mavlink.Dialects.Infrastructure.MavlinkEnum("MAV_STATE")]
			public enum MavState : sbyte { Uninit = 0, Negative = -1 }

			public enum NotFromXml { A }

			[global::Mavlink.Dialects.Infrastructure.MavlinkEnum("HIDDEN")]
			internal enum Hidden { A }
		}
		""";

	[Fact]
	public void Read_Manifest()
	{
		var result = Read(Compile("Mavlink.Dialects.Minimal", MinimalSource));

		result.Diagnostics.Should().BeEmpty();
		var dialect = result.Dialects.Should().ContainSingle().Subject;
		dialect.Name.Should().Be("minimal");
		dialect.Namespace.Should().Be("Mavlink.Minimal");
		dialect.DialectType.Should().Be("global::Mavlink.Dialects.MinimalDialect");
		dialect.Includes.Should().BeEmpty();
		dialect.XmlHash.Should().Be("abc");
		dialect.Assembly.Should().Be("Mavlink.Dialects.Minimal");
	}

	[Fact]
	public void Read_Enums()
	{
		var enums = Read(Compile("Mavlink.Dialects.Minimal", MinimalSource)).Dialects[0].Enums;

		enums.Select(e => e.XmlName).Should().Equal("MAV_MODE_FLAG", "MAV_STATE");

		var flags = enums[0];
		(flags.Namespace, flags.TypeName, flags.Underlying, flags.HasBitmask)
			.Should().Be(("Mavlink.Minimal", "MavModeFlag", WireType.UInt8, true));
		flags.Members.Should().Equal(new EnumMemberFacts("CustomModeEnabled", 1), new EnumMemberFacts("SafetyArmed", 128));

		var state = enums[1];
		(state.Underlying, state.HasBitmask).Should().Be((WireType.Int8, false));
		state.Members.Should().Equal(new EnumMemberFacts("Uninit", 0), new EnumMemberFacts("Negative", ulong.MaxValue));
	}

	[Fact]
	public void Read_IncludesAndSeveralAssemblies()
	{
		var minimal = Compile("Mavlink.Dialects.Minimal", MinimalSource);
		var common = Compile("Mavlink.Dialects.Common", """
			[assembly: global::Mavlink.Dialects.Infrastructure.MavlinkDialectManifest(
				1, "common", "Mavlink.Common", typeof(global::Mavlink.Dialects.CommonDialect),
				Includes = new[] { "minimal" })]

			namespace Mavlink.Dialects { public sealed class CommonDialect { } }
			""");

		var result = Read(common, minimal);

		result.Diagnostics.Should().BeEmpty();
		result.Dialects.Select(d => d.Name).Should().Equal("common", "minimal");
		result.Dialects[0].Includes.Should().Equal("minimal");
		result.Dialects[0].XmlHash.Should().BeNull();
		result.Dialects[0].Enums.Should().BeEmpty();
	}

	[Fact]
	public void Read_SameDialectInTwoAssemblies_KeepsTheFirstByName()
	{
		var result = Read(Compile("B.Dialects", MinimalSource), Compile("A.Dialects", MinimalSource));

		var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.DuplicateReferencedDialect);
		diagnostic.MessageArgs.Should().Equal("minimal", "A.Dialects", "B.Dialects");
		result.Dialects.Should().ContainSingle().Which.Assembly.Should().Be("A.Dialects");
	}

	[Fact]
	public void Read_UnknownFormat_IsReportedAndSkipped()
	{
		var result = Read(Compile("Future.Dialects", MinimalSource.Replace("1, \"minimal\"", "2, \"minimal\"", StringComparison.Ordinal)));

		var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.UnsupportedManifest);
		diagnostic.MessageArgs.Should().Equal("Future.Dialects", "minimal", "2", "1");
		result.Dialects.Should().BeEmpty();
	}

	[Fact]
	public void Read_IncludeMissingFromReferences_IsAWarning()
	{
		var common = Compile("Mavlink.Dialects.Common", """
			[assembly: global::Mavlink.Dialects.Infrastructure.MavlinkDialectManifest(
				1, "common", "Mavlink.Common", typeof(global::Mavlink.Dialects.CommonDialect),
				Includes = new[] { "minimal" })]

			namespace Mavlink.Dialects { public sealed class CommonDialect { } }
			""");

		var result = Read(common);

		var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.ReferencedIncludeMissing);
		diagnostic.Descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
		diagnostic.MessageArgs.Should().Equal("common", "Mavlink.Dialects.Common", "minimal");
		result.Dialects.Should().ContainSingle();
	}

	[Fact]
	public void Read_WithoutDialectReferences_IsEmpty()
	{
		var result = Read(Compile("Unrelated", "namespace Unrelated { public static class A { } }"));

		result.Should().Be(ReferencedDialects.Empty);
	}

	[Fact]
	public void Read_SameReferences_IsEqual()
	{
		var minimal = Compile("Mavlink.Dialects.Minimal", MinimalSource);

		Read(minimal).Should().Be(Read(minimal));
	}

	internal static MetadataReference Compile(string assemblyName, string source)
		=> GeneratorHarness.Run(new GeneratorInput { AssemblyName = assemblyName }.WithSource(source)).ToReference();

	private static ReferencedDialects Read(params MetadataReference[] references)
	{
		var input = references.Aggregate(new GeneratorInput { AssemblyName = "Consumer" }, (i, r) => i.WithReference(r));
		return ReferencedDialectReader.Read(GeneratorHarness.Run(input).OutputCompilation, CancellationToken.None);
	}
}
