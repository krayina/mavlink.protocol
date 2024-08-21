namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a field type in a Mavlink message with its original Mavlink type name.
/// </summary>
/// <remarks>
/// The <see cref="TypeName"/> property contains the type information as defined in the Mavlink specification.
/// This can represent both a single type (e.g., "uint8_t") and an array type (e.g., "uint8_t[4]").
/// </remarks>
public record MavlinkMessageFieldType(string TypeName);

/// <summary>
/// Represents an enum field type in a Mavlink message with its original enum name.
/// </summary>
/// <remarks>
/// The <see cref="MavlinkMessageFieldType.TypeName"/> property indicates the size of the enum type (e.g., "uint8_t" or "uint8_t[4]").
/// The <see cref="EnumName"/> property contains the original name of the enum as defined in the Mavlink specification,
/// which can also represent an array of enum values.
/// </remarks>
public record MavlinkMessageFieldEnumType(string TypeName, string EnumName) : MavlinkMessageFieldType(TypeName);
