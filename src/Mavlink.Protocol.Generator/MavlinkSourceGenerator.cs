using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Linking;
using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Entry point of the MAVLink source generator. Only wires the incremental pipeline;
/// parsing, resolution and emission live in their own stages.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MavlinkSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Where(static pair => DialectFiles.IsOwned(pair.Left, pair.Right))
			.Select(static (pair, _) => pair.Left)
			.Select(static (file, cancellationToken) => DialectFiles.Read(file, cancellationToken))
			.WithTrackingName(TrackingNames.Parse);

		// Runs on every compilation change, but the result is equatable, so the steps after it
		// only rerun when the referenced dialects change.
		var references = context.CompilationProvider
			.Select(static (compilation, cancellationToken) => ReferencedDialectReader.Read(compilation, cancellationToken))
			.WithTrackingName(TrackingNames.References);

		var graph = files
			.Collect()
			.Combine(references)
			.Select(static (input, cancellationToken) => DialectLinker.Link(input.Left, input.Right.Dialects.AsImmutableArray(), cancellationToken))
			.WithTrackingName(TrackingNames.Link);

		context.RegisterSourceOutput(files, static (context, file) => Report(context, file.Diagnostics));
		context.RegisterSourceOutput(references, static (context, references) => Report(context, references.Diagnostics));
		context.RegisterSourceOutput(graph, static (context, graph) => Report(context, graph.Diagnostics));
	}

	private static void Report(SourceProductionContext context, EquatableArray<DiagnosticInfo> diagnostics)
	{
		foreach (var diagnostic in diagnostics)
		{
			context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}
	}
}

/// <summary>
/// Names of pipeline steps, for incremental caching tests.
/// </summary>
internal static class TrackingNames
{
	public const string Parse = nameof(Parse);
	public const string References = nameof(References);
	public const string Link = nameof(Link);
}
