using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mavlink.Protocol.Generator.Tests.Harness;

/// <summary>
/// MSBuild-like options: global properties (<c>build_property.X</c>) and per-file item
/// metadata (<c>build_metadata.AdditionalFiles.X</c>), as the SDK passes them to generators.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
	private readonly ImmutableDictionary<string, TestAnalyzerConfigOptions> _perFile;

	public TestAnalyzerConfigOptionsProvider(
		ImmutableDictionary<string, string> globalOptions,
		ImmutableDictionary<string, ImmutableDictionary<string, string>> perFileOptions)
	{
		GlobalOptions = new TestAnalyzerConfigOptions(globalOptions);
		_perFile = perFileOptions.ToImmutableDictionary(p => p.Key, p => new TestAnalyzerConfigOptions(p.Value));
	}

	public override AnalyzerConfigOptions GlobalOptions { get; }

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		=> _perFile.TryGetValue(textFile.Path, out var options) ? options : TestAnalyzerConfigOptions.Empty;
}

internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
	public static readonly TestAnalyzerConfigOptions Empty = new(ImmutableDictionary<string, string>.Empty);

	private readonly ImmutableDictionary<string, string> _values;

	public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> values)
	{
		_values = values;
	}

	public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _values.TryGetValue(key, out value);
}
