namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a standard dotnet type that has been converted from a mavlink type.
/// </summary>
/// <remarks>
/// type="uint8_t" will be converted to byte
/// </remarks>
public record MavlinkMessageFieldType(string TypeName);

/// <summary>
/// Represents an array with standard dotnet type and and array length.
/// </summary>
/// <remarks>
/// type="uint32_t[4]" will be converted to uint with array length 4 
/// </remarks>
public record MavlinkMessageFieldArrayType(string TypeName, int ArrayLength) : MavlinkMessageFieldType(TypeName);

/// <summary>
/// Represents an enum field type with a name and size.
/// </summary>
/// <remarks>
/// type="uint8_t" enum=""ESC_CONNECTION_TYPE"" will be converted to original enum name ESC_CONNECTION_TYPE<br/>
/// with real enum size -- sizeof(byte)
/// </remarks>
public record MavlinkMessageFieldEnumType(string TypeName, int EnumSize) : MavlinkMessageFieldType(TypeName);

/// <summary>
/// Represents an enum field type with a name and size.
/// </summary>
/// <remarks>
/// type="uint16_t[4]" enum=""ESC_FAILURE_FLAGS"" will be converted to original enum name ESC_FAILURE_FLAGS<br/>
/// with real enum size -- sizeof(ushort) and array length 4
/// </remarks>
public record MavlinkMessageFieldArrayEnumType(string TypeName, int EnumSize, int ArrayLength) : MavlinkMessageFieldType(TypeName);
