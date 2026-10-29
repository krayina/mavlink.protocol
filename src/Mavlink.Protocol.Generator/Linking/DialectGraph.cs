using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;

namespace Mavlink.Protocol.Generator.Linking;

internal enum DialectOrigin
{
	/// <summary>A <c>MavlinkDialect</c> item of this project.</summary>
	Own,

	/// <summary>A dialect from a referenced assembly.</summary>
	Referenced,
}

internal readonly record struct DialectReference(string Name, DialectOrigin Origin);

/// <summary>
/// An own dialect file with its includes resolved.
/// </summary>
/// <param name="Includes">Direct includes in XML order; unresolved ones and the include that closes a cycle are left out.</param>
/// <param name="Closure">Every dialect reachable through includes, nearest first, without the dialect itself.</param>
internal sealed record LinkedDialect(
	string Name,
	ParsedFile File,
	EquatableArray<DialectReference> Includes,
	EquatableArray<DialectReference> Closure);

/// <param name="Dialects">Own dialects, each after the own dialects it includes.</param>
internal sealed record DialectGraph(
	EquatableArray<LinkedDialect> Dialects,
	EquatableArray<DiagnosticInfo> Diagnostics);
