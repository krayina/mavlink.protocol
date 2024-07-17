using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public record FieldType(string TypeName);

public record FieldArrayType(string TypeName, int Length) : FieldType(TypeName);

public record FieldEnumType(string TypeName, int Size) : FieldType(TypeName);

public record MavlinkData(
	ImmutableArray<(string Name, string? Description, bool Bitmask, ImmutableList<(string Name, string Value, string? Description)> Entries)> Enums,
	ImmutableArray<(uint Id, string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> Messages,
	ImmutableArray<string> Includes,
	byte? Version,
	byte? Dialect);
