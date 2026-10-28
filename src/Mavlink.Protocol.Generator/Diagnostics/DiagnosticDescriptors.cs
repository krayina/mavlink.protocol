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
}
