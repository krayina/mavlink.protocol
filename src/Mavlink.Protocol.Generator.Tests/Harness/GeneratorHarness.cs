using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mavlink.Protocol.Generator.Tests.Harness;

/// <summary>
/// Runs <see cref="MavlinkSourceGenerator"/> in memory, the way the compiler runs it inside a
/// project, and compiles the result against the runtime and Mavlink.Protocol.
/// </summary>
internal static class GeneratorHarness
{
	private static readonly Lazy<ImmutableArray<MetadataReference>> DefaultReferences = new(LoadDefaultReferences);

	public static GeneratorRun Run(GeneratorInput input)
	{
		var parseOptions = new CSharpParseOptions(input.LanguageVersion);

		var compilation = CSharpCompilation.Create(
			input.AssemblyName,
			input.Sources.Select(code => CSharpSyntaxTree.ParseText(code, parseOptions)),
			DefaultReferences.Value.AddRange(input.References),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		var additionalTexts = input.Files
			.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content))
			.ToImmutableArray();

		var optionsProvider = new TestAnalyzerConfigOptionsProvider(
			input.GlobalOptions,
			input.Files.ToImmutableDictionary(
				file => file.Path,
				file => file.Metadata.ToImmutableDictionary(m => GeneratorInput.ItemMetadataPrefix + m.Key, m => m.Value)));

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new MavlinkSourceGenerator().AsSourceGenerator() },
			additionalTexts,
			parseOptions,
			optionsProvider,
			new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		return GeneratorRun.Execute(driver, compilation, additionalTexts);
	}

	private static ImmutableArray<MetadataReference> LoadDefaultReferences()
	{
		// The test host runs on the same runtime the generated code targets here (net10.0).
		var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

		var core = typeof(global::Mavlink.Dialects.IMavlinkDialect).Assembly.Location;

		return trustedAssemblies
			.Where(path => !string.Equals(path, core, StringComparison.OrdinalIgnoreCase))
			.Append(core)
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
			.ToImmutableArray();
	}
}
