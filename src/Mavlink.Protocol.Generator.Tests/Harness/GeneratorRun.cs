using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator.Tests.Harness;

/// <summary>
/// Result of one generator pass: generated sources, generator diagnostics, the compilation
/// that includes the generated code, and the incremental steps for caching checks.
/// </summary>
internal sealed class GeneratorRun
{
	private readonly GeneratorDriver _driver;
	private readonly Compilation _inputCompilation;
	private readonly ImmutableArray<AdditionalText> _additionalTexts;

	private GeneratorRun(
		GeneratorDriver driver,
		Compilation inputCompilation,
		ImmutableArray<AdditionalText> additionalTexts,
		Compilation outputCompilation,
		ImmutableArray<Diagnostic> generatorDiagnostics)
	{
		_driver = driver;
		_inputCompilation = inputCompilation;
		_additionalTexts = additionalTexts;
		OutputCompilation = outputCompilation;
		GeneratorDiagnostics = generatorDiagnostics;
		Result = driver.GetRunResult().Results.Single();
	}

	/// <summary>
	/// The project compilation with the generated sources added.
	/// </summary>
	public Compilation OutputCompilation { get; }

	/// <summary>
	/// Diagnostics reported by the generator itself (MAVGENxxx), including generator crashes.
	/// </summary>
	public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

	public GeneratorRunResult Result { get; }

	public ImmutableArray<GeneratedSourceResult> GeneratedSources => Result.GeneratedSources;

	/// <summary>
	/// Warnings and errors of the compilation that includes the generated code.
	/// </summary>
	public ImmutableArray<Diagnostic> CompilationDiagnostics
		=> OutputCompilation.GetDiagnostics()
			.Where(d => d.Severity >= DiagnosticSeverity.Warning)
			.ToImmutableArray();

	public string GetSource(string hintName)
		=> GeneratedSources.Single(s => s.HintName == hintName).SourceText.ToString();

	/// <summary>
	/// Runs the generator again on unchanged input, to check what was served from the cache.
	/// </summary>
	public GeneratorRun RunAgain() => Execute(_driver, _inputCompilation, _additionalTexts);

	/// <summary>
	/// Runs the generator again with one XML file's content replaced.
	/// </summary>
	public GeneratorRun ReplaceXml(string path, string content)
	{
		var old = _additionalTexts.Single(t => t.Path == path);
		var replacement = new TestAdditionalText(path, content);
		var driver = _driver.ReplaceAdditionalText(old, replacement);
		return Execute(driver, _inputCompilation, _additionalTexts.Replace(old, replacement));
	}

	/// <summary>
	/// Emits the output compilation, e.g. to reference it as a dialect assembly from another run.
	/// </summary>
	public MetadataReference ToReference()
	{
		using var stream = new MemoryStream();
		var emit = OutputCompilation.Emit(stream);
		if (!emit.Success)
		{
			throw new InvalidOperationException(
				"The generated code does not compile:" + Environment.NewLine +
				string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
		}

		return MetadataReference.CreateFromImage(stream.ToArray());
	}

	internal static GeneratorRun Execute(
		GeneratorDriver driver,
		Compilation inputCompilation,
		ImmutableArray<AdditionalText> additionalTexts)
	{
		driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diagnostics);
		return new GeneratorRun(driver, inputCompilation, additionalTexts, output, diagnostics);
	}
}
