using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;

namespace Mavlink.Protocol.Generator.Parsing;

// A mirror of one dialect XML file. Values the generator decides on (ids, enum values,
// indexes, flags) are parsed; values it only reads later or prints (types, units, invalid,
// ranges, descriptions) stay as the original text. No MAVLink rules are applied here.

/// <param name="Hash">SHA-256 of the file text, see <see cref="XmlContentHash"/>.</param>
/// <param name="Diagnostics">Problems found while reading; elements they refer to are skipped.</param>
internal sealed record ParsedFile(
	string Path,
	string Hash,
	EquatableArray<ParsedInclude> Includes,
	byte? Version,
	byte? Dialect,
	EquatableArray<ParsedEnum> Enums,
	EquatableArray<ParsedMessage> Messages,
	EquatableArray<DiagnosticInfo> Diagnostics);

/// <param name="Path">The include as written, e.g. <c>common.xml</c>.</param>
internal sealed record ParsedInclude(string Path, LocationInfo Location);

internal enum ParsedStatusKind
{
	Deprecated,
	Superseded,
	Wip,
}

/// <summary>
/// One of <c>&lt;deprecated&gt;</c>, <c>&lt;superseded&gt;</c> or <c>&lt;wip&gt;</c>.
/// </summary>
internal sealed record ParsedStatus(
	ParsedStatusKind Kind,
	string? Since,
	string? ReplacedBy,
	string? RemoveOnDate,
	string? Description);

internal sealed record ParsedEnum(
	string Name,
	bool IsBitmask,
	string? Description,
	ParsedStatus? Status,
	EquatableArray<ParsedEntry> Entries,
	LocationInfo Location);

/// <param name="Value">Null when the XML omits it; how to fill it in is decided later.</param>
internal sealed record ParsedEntry(
	string Name,
	ulong? Value,
	string? Description,
	ParsedStatus? Status,
	ParsedEntryFlags Flags,
	EquatableArray<ParsedParam> Params,
	LocationInfo Location);

/// <summary>
/// Boolean attributes of <c>&lt;entry&gt;</c>.
/// </summary>
[Flags]
internal enum ParsedEntryFlags
{
	None = 0,
	HasLocation = 1 << 0,
	IsDestination = 1 << 1,
	HasAltitudeOnly = 1 << 2,
	MissionOnly = 1 << 3,
	Mission = 1 << 4,
	Fence = 1 << 5,
	Rally = 1 << 6,
	Command = 1 << 7,
}

internal sealed record ParsedParam(
	byte Index,
	string? Description,
	string? Label,
	string? Units,
	string? Multiplier,
	bool Instance,
	string? Enum,
	string? DecimalPlaces,
	string? Increment,
	string? MinValue,
	string? MaxValue,
	bool Reserved,
	string? Default,
	LocationInfo Location);

/// <param name="Fields">XML order; extension fields follow the base ones.</param>
internal sealed record ParsedMessage(
	uint Id,
	string Name,
	string? Description,
	ParsedStatus? Status,
	EquatableArray<ParsedField> Fields,
	LocationInfo Location);

/// <param name="Type">As written, e.g. <c>uint8_t[16]</c>.</param>
internal sealed record ParsedField(
	string Type,
	string Name,
	bool IsExtension,
	string? Description,
	string? Enum,
	string? Display,
	string? Units,
	string? Multiplier,
	string? PrintFormat,
	string? Invalid,
	string? Default,
	bool Instance,
	string? Increment,
	string? MinValue,
	string? MaxValue,
	LocationInfo Location);
