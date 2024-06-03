using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public static class MavlinkGeneratorDiagnostics
{
	internal const string Title = "Mavlink type parse failed";
	internal const string MessageFormat = "{0}";
	internal const string Category = "Protocol";
	internal const string GenerationFailureDescription = "Mavlink type parse failed";
	public static readonly DiagnosticDescriptor GenericProtocolErrorRule = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"MavXmlParse",
#pragma warning restore RS2008 // Enable analyzer release tracking
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
#pragma warning disable RS1033 // Define diagnostic description correctly
		description: GenerationFailureDescription
#pragma warning restore RS1033 // Define diagnostic description correctly
		);
}
