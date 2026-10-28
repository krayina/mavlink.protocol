using AwesomeAssertions;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Tests.Harness;
using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator.Tests;

/// <summary>
/// The generator as the compiler runs it: XML and include problems reach the build output.
/// </summary>
public sealed class GeneratorDiagnosticsTests
{
	private static readonly (string, string) Own = (DialectFiles.MetadataName, "true");

	private const string Minimal = "<mavlink>\n  <messages/>\n</mavlink>\n";

	private const string Common = "<mavlink>\n  <include>minimal.xml</include>\n  <messages/>\n</mavlink>\n";

	[Fact]
	public void Run_ValidDialects_ReportsNothing()
	{
		var run = GeneratorHarness.Run(new GeneratorInput()
			.WithXml("/x/minimal.xml", Minimal, Own)
			.WithXml("/x/common.xml", Common, Own));

		run.Result.Exception.Should().BeNull();
		run.GeneratorDiagnostics.Should().BeEmpty();
	}

	[Fact]
	public void Run_BrokenXml_IsReportedInTheXmlFile()
	{
		var run = GeneratorHarness.Run(new GeneratorInput()
			.WithXml("/x/minimal.xml", "<mavlink>\n  <messages>\n</mavlink>\n", Own));

		run.Result.Exception.Should().BeNull();
		var diagnostic = run.GeneratorDiagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("MAVGEN001");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
		diagnostic.Location.Kind.Should().Be(LocationKind.ExternalFile);
		diagnostic.Location.GetLineSpan().Path.Should().Be("/x/minimal.xml");
	}

	[Fact]
	public void Run_MissingInclude_IsReported()
	{
		var run = GeneratorHarness.Run(new GeneratorInput().WithXml("/x/common.xml", Common, Own));

		run.GeneratorDiagnostics.Should().ContainSingle().Which.Id.Should().Be("MAVGEN002");
	}

	[Fact]
	public void Run_FileWithoutMetadata_IsIgnored()
	{
		var run = GeneratorHarness.Run(new GeneratorInput()
			.WithXml("/x/broken.xml", "<not xml")
			.WithXml("/x/common.xml", Common, Own)
			.WithXml("/x/minimal.xml", "<not xml", (DialectFiles.MetadataName, "false")));

		// minimal.xml is not an own file, so common.xml cannot find it.
		run.GeneratorDiagnostics.Select(d => d.Id).Should().Equal("MAVGEN002");
	}

	[Fact]
	public void RunAgain_UnchangedInput_IsServedFromCache()
	{
		var run = GeneratorHarness.Run(new GeneratorInput()
			.WithXml("/x/minimal.xml", Minimal, Own)
			.WithXml("/x/common.xml", Common, Own));

		var rerun = run.RunAgain();

		Reasons(rerun, TrackingNames.Parse).Should().AllBeEquivalentTo(IncrementalStepRunReason.Cached);
		Reasons(rerun, TrackingNames.Link).Should().AllBeEquivalentTo(IncrementalStepRunReason.Cached);
	}

	[Fact]
	public void ReplaceXml_ReparsesOnlyThatFile()
	{
		var run = GeneratorHarness.Run(new GeneratorInput()
			.WithXml("/x/minimal.xml", Minimal, Own)
			.WithXml("/x/common.xml", Common, Own));

		var rerun = run.ReplaceXml("/x/common.xml", Common.Replace("<messages/>", "<messages></messages>", StringComparison.Ordinal));

		Reasons(rerun, TrackingNames.Parse).Should().BeEquivalentTo(
			[IncrementalStepRunReason.Cached, IncrementalStepRunReason.Modified]);
		Reasons(rerun, TrackingNames.Link).Should().Equal(IncrementalStepRunReason.Modified);
	}

	private static IEnumerable<IncrementalStepRunReason> Reasons(GeneratorRun run, string step)
		=> run.Result.TrackedSteps[step].SelectMany(s => s.Outputs).Select(o => o.Reason);
}
