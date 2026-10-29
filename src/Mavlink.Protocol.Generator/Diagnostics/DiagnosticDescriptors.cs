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

	public static readonly DiagnosticDescriptor DuplicateReferencedDialect = new(
		id: "MAVGEN009",
		title: "Dialect provided by two referenced assemblies",
		messageFormat: "Dialect '{0}' is provided by two referenced assemblies, '{1}' and '{2}'; the one from '{2}' is ignored",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor LocalCopyDiffers = new(
		id: "MAVGEN010",
		title: "Local dialect file differs from the referenced one",
		messageFormat: "Dialect '{0}' is already provided by referenced assembly '{1}', which was generated from a different version of this file; remove the local copy or regenerate both from the same XML",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedManifest = new(
		id: "MAVGEN011",
		title: "Unsupported dialect manifest",
		messageFormat: "Referenced assembly '{0}' describes dialect '{1}' in manifest format {2}, but this generator reads format {3}; use the same generator version for both projects",
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

	public static readonly DiagnosticDescriptor LocalCopyIgnored = new(
		id: "MAVGEN102",
		title: "Local dialect file ignored",
		messageFormat: "Dialect '{0}' is already provided by referenced assembly '{1}', generated from the same file; the local copy is ignored",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor ReferencedIncludeMissing = new(
		id: "MAVGEN106",
		title: "Included dialect of a reference not found",
		messageFormat: "Dialect '{0}' from '{1}' includes '{2}', which no referenced assembly provides; reference the assembly that owns '{2}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);
}
