using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator.Diagnostics;

/// <summary>
/// All diagnostics of the generator. Every new rule is also listed in AnalyzerReleases.Unshipped.md.
/// </summary>
internal static class DiagnosticDescriptors
{
	private const string Category = "MavlinkGenerator";

	public static readonly DiagnosticDescriptor InvalidXml = new(
		id: "MAVGEN001",
		title: "Invalid MAVLink XML",
		messageFormat: "{0}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnresolvedInclude = new(
		id: "MAVGEN002",
		title: "Included dialect not found",
		messageFormat: "'{0}' includes '{1}', which is neither a dialect of this project (a MavlinkDialect item) nor provided by a referenced dialect assembly",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor IncludeCycle = new(
		id: "MAVGEN003",
		title: "Include cycle",
		messageFormat: "Include cycle: {0}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DuplicateDialectFile = new(
		id: "MAVGEN014",
		title: "Two dialect files with the same name",
		messageFormat: "Dialect '{0}' is already defined by '{1}'; this file is ignored. A dialect is identified by its file name, so two files cannot share it.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
