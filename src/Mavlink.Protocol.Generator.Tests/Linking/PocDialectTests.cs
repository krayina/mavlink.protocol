using System.Collections.Immutable;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Parsing;
using Mavlink.Protocol.Generator.Tests.Harness;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Linking;

/// <summary>
/// The PoC assembly as a referenced dialect assembly: its manifests are the hand-written
/// target of the generator, and their hashes match the official XMLs in the repository.
/// </summary>
public sealed class PocDialectTests
{
	private static readonly MetadataReference Poc =
		MetadataReference.CreateFromFile(typeof(global::Mavlink.Dialects.CommonDialect).Assembly.Location);

	private static readonly string[] ArdupilotmegaTree =
		["ardupilotmega.xml", "common.xml", "uAvionix.xml", "icarous.xml", "cubepilot.xml", "csAirLink.xml"];

	[Fact]
	public void Read_FindsMinimalAndCommon()
	{
		var result = Read();

		result.Diagnostics.Should().BeEmpty();
		result.Dialects.Select(d => (d.Name, d.Namespace, d.DialectType)).Should().Equal(
			("common", "Mavlink.Common", "global::Mavlink.Dialects.CommonDialect"),
			("minimal", "Mavlink.Minimal", "global::Mavlink.Dialects.MinimalDialect"));
		result.Dialects[0].Includes.Should().Equal("minimal");
	}

	[Fact]
	public void Read_Enums()
	{
		var minimal = Read().Dialects.Single(d => d.Name == "minimal");

		minimal.Enums.Select(e => (e.XmlName, e.TypeName, e.Underlying, e.HasBitmask)).Should().Equal(
			("MAV_AUTOPILOT", "MavAutopilot", WireType.UInt8, false),
			("MAV_MODE_FLAG", "MavModeFlag", WireType.UInt8, true),
			("MAV_STATE", "MavState", WireType.UInt8, false),
			("MAV_TYPE", "MavType", WireType.UInt8, false));
	}

	[Theory]
	[InlineData("minimal.xml")]
	[InlineData("common.xml")]
	public void Read_HashMatchesTheOfficialXml(string fileName)
	{
		var dialect = Read().Dialects.Single(d => d.Name == DialectName.FromPath(fileName));

		dialect.XmlHash.Should().Be(XmlContentHash.Compute(File.ReadAllText(TestData.XmlPath(fileName))));
	}

	[Fact]
	public void Link_OfficialArdupilotmegaAgainstPoc_UsesTheCompiledCommon()
	{
		var own = ArdupilotmegaTree
			.Select(name => MavlinkXmlReader.Read(TestData.XmlPath(name), SourceText.From(File.ReadAllText(TestData.XmlPath(name))), CancellationToken.None))
			.ToImmutableArray();

		var graph = DialectLinker.Link(own, Read().Dialects.AsImmutableArray(), CancellationToken.None);

		// common.xml is the same file the PoC was made from, so the local copy gives way.
		graph.Diagnostics.Select(d => (d.Descriptor, d.Location!.FilePath)).Should().Equal(
			(DiagnosticDescriptors.LocalCopyIgnored, TestData.XmlPath("common.xml")));
		graph.Dialects.Single(d => d.Name == "ardupilotmega").Closure.Should().Equal(
			new DialectReference("common", DialectOrigin.Referenced),
			new DialectReference("uAvionix", DialectOrigin.Own),
			new DialectReference("icarous", DialectOrigin.Own),
			new DialectReference("cubepilot", DialectOrigin.Own),
			new DialectReference("csAirLink", DialectOrigin.Own),
			new DialectReference("minimal", DialectOrigin.Referenced));
	}

	private static ReferencedDialects Read()
		=> ReferencedDialectReader.Read(
			GeneratorHarness.Run(new GeneratorInput().WithReference(Poc)).OutputCompilation,
			CancellationToken.None);
}
