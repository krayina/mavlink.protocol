using AwesomeAssertions;
using Mavlink.Protocol.Generator.Tests.Harness;

namespace Mavlink.Protocol.Generator.Tests;

/// <summary>
/// Checks the harness itself and the empty generator: it loads, runs and adds nothing.
/// </summary>
public sealed class HarnessTests
{
	private const string MinimalXml = """
		<?xml version="1.0"?>
		<mavlink>
		  <version>3</version>
		  <messages/>
		</mavlink>
		""";

	[Fact]
	public void Run_WithoutInput_ProducesNothing()
	{
		var run = GeneratorHarness.Run(new GeneratorInput());

		run.Result.Exception.Should().BeNull();
		run.GeneratorDiagnostics.Should().BeEmpty();
		run.GeneratedSources.Should().BeEmpty();
		run.CompilationDiagnostics.Should().BeEmpty();
	}

	[Fact]
	public void Run_WithXmlAndMetadata_DoesNotFail()
	{
		var input = new GeneratorInput()
			.WithXml("/dialects/minimal.xml", MinimalXml, ("MavlinkDialect", "true"))
			.WithProperty("RootNamespace", "Test")
			.WithSource("namespace Test { public static class Placeholder { } }");

		var run = GeneratorHarness.Run(input);

		run.Result.Exception.Should().BeNull();
		run.GeneratorDiagnostics.Should().BeEmpty();
		run.CompilationDiagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ToReference_EmitsAnAssemblyAnotherRunCanReference()
	{
		var first = GeneratorHarness.Run(new GeneratorInput()
			.WithSource("namespace Upstream { public static class Marker { } }"));

		var second = GeneratorHarness.Run(new GeneratorInput { AssemblyName = "Downstream" }
			.WithReference(first.ToReference())
			.WithSource("namespace Downstream { public static class User { public static string Name => nameof(Upstream.Marker); } }"));

		second.CompilationDiagnostics.Should().BeEmpty();
	}

	[Fact]
	public void AddSource_CompilesTheNewSource()
	{
		var run = GeneratorHarness.Run(new GeneratorInput().WithSource("namespace Test { public static class A { } }"));

		var rerun = run.AddSource("namespace Test { public static class B { public static string Name => nameof(A); } }");

		rerun.Result.Exception.Should().BeNull();
		rerun.CompilationDiagnostics.Should().BeEmpty();
		rerun.OutputCompilation.GetTypeByMetadataName("Test.B").Should().NotBeNull();
	}

	[Fact]
	public void ReplaceXml_RunsAgainWithNewContent()
	{
		var run = GeneratorHarness.Run(new GeneratorInput().WithXml("/dialects/minimal.xml", MinimalXml));

		var rerun = run.ReplaceXml("/dialects/minimal.xml", MinimalXml.Replace("<version>3</version>", "<version>4</version>", StringComparison.Ordinal));

		rerun.Result.Exception.Should().BeNull();
		rerun.RunAgain().Result.Exception.Should().BeNull();
	}
}
