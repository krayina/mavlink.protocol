using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;

namespace Mavlink.Protocol.Generator.Linking;

/// <summary>
/// A dialect compiled into a referenced assembly, as its manifest and enums describe it.
/// </summary>
/// <param name="Name">XML file name without the extension, e.g. <c>common</c>.</param>
/// <param name="Namespace">Namespace of the dialect's types, e.g. <c>Mavlink.Common</c>.</param>
/// <param name="DialectType">Fully qualified dialect class, e.g. <c>global::Mavlink.Dialects.CommonDialect</c>.</param>
/// <param name="Includes">Names of the dialects it includes directly.</param>
/// <param name="XmlHash">Hash of the XML it was generated from; null if the manifest has none.</param>
/// <param name="Assembly">Name of the assembly that owns the dialect.</param>
internal sealed record ReferencedDialect(
	string Name,
	string Namespace,
	string DialectType,
	EquatableArray<string> Includes,
	string? XmlHash,
	string Assembly,
	EquatableArray<EnumFacts> Enums);

/// <summary>
/// What a dialect needs to know about an enum declared in another dialect: its C# type,
/// width, whether a bitmask type exists, and its members (for derived enums).
/// </summary>
/// <param name="XmlName">Name in the XML, e.g. <c>MAV_TYPE</c>.</param>
/// <param name="Namespace">Namespace of the enum, e.g. <c>Mavlink.Minimal</c>.</param>
/// <param name="TypeName">C# name of the enum, e.g. <c>MavType</c>.</param>
/// <param name="HasBitmask">Whether <c>&lt;TypeName&gt;Bitmask</c> exists next to the enum.</param>
/// <param name="Members">Members in declaration order.</param>
internal sealed record EnumFacts(
	string XmlName,
	string Namespace,
	string TypeName,
	WireType Underlying,
	bool HasBitmask,
	EquatableArray<EnumMemberFacts> Members);

/// <param name="Value">The constant as an unsigned 64-bit number (signed values are reinterpreted).</param>
internal readonly record struct EnumMemberFacts(string Name, ulong Value);

/// <summary>
/// All dialects found in the references, with problems found while reading them.
/// </summary>
internal sealed record ReferencedDialects(
	EquatableArray<ReferencedDialect> Dialects,
	EquatableArray<DiagnosticInfo> Diagnostics)
{
	public static readonly ReferencedDialects Empty = new(EquatableArray<ReferencedDialect>.Empty, EquatableArray<DiagnosticInfo>.Empty);
}
