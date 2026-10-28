using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mavlink.Protocol.Generator.Tests.Harness;

/// <summary>
/// Everything a project hands to the generator: dialect XML items with their metadata,
/// global MSBuild properties, the project's own sources and its references.
/// </summary>
internal sealed record GeneratorInput
{
	/// <summary>
	/// Prefix of the item metadata keys the SDK passes for AdditionalFiles items.
	/// </summary>
	public const string ItemMetadataPrefix = "build_metadata.AdditionalFiles.";

	/// <summary>
	/// Prefix of the global MSBuild property keys visible to generators.
	/// </summary>
	public const string PropertyPrefix = "build_property.";

	public string AssemblyName { get; init; } = "TestDialects";

	public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.Latest;

	public ImmutableArray<TestXml> Files { get; init; } = ImmutableArray<TestXml>.Empty;

	public ImmutableDictionary<string, string> GlobalOptions { get; init; } = ImmutableDictionary<string, string>.Empty;

	public ImmutableArray<string> Sources { get; init; } = ImmutableArray<string>.Empty;

	/// <summary>
	/// Extra references on top of the runtime and Mavlink.Protocol, e.g. a dialect assembly
	/// produced by another run (<see cref="GeneratorRun.ToReference"/>).
	/// </summary>
	public ImmutableArray<MetadataReference> References { get; init; } = ImmutableArray<MetadataReference>.Empty;

	public GeneratorInput WithXml(string path, string content, params (string Key, string Value)[] metadata)
		=> this with { Files = Files.Add(new TestXml(path, content, metadata.ToImmutableDictionary(m => m.Key, m => m.Value))) };

	public GeneratorInput WithProperty(string name, string value)
		=> this with { GlobalOptions = GlobalOptions.SetItem(PropertyPrefix + name, value) };

	public GeneratorInput WithSource(string code)
		=> this with { Sources = Sources.Add(code) };

	public GeneratorInput WithReference(MetadataReference reference)
		=> this with { References = References.Add(reference) };
}

/// <summary>
/// One AdditionalFiles item. <see cref="Metadata"/> holds item metadata by name (without the prefix).
/// </summary>
internal sealed record TestXml(string Path, string Content, ImmutableDictionary<string, string> Metadata);
