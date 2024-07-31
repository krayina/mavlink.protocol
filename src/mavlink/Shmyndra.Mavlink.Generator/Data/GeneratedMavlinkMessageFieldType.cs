namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a field type in a Mavlink message with a corresponding dotnet type conversion.<br/>
/// For a Mavlink type "uint8_t", the <see cref="MavlinkMessageFieldType.TypeName"/> property would be "uint8_t" and the <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> property would be "byte".
/// </summary>
/// <param name="TypeName">The name of the original Mavlink type.</param>
/// <param name="ConvertedType">The name of the corresponding converted dotnet type.</param>
/// <remarks>
/// <see cref="MavlinkMessageFieldType.TypeName"/> can represent a standard Mavlink type or an array type.
/// </remarks>
public record GeneratedMavlinkMessageFieldType(string TypeName, string ConvertedType) : MavlinkMessageFieldType(TypeName);

/// <summary>
/// Represents a generated Mavlink message field array type with an additional converted type.<br/>
/// For a Mavlink type "uint32_t[4]", the <see cref="MavlinkMessageFieldType.TypeName"/> property would be "uint32_t[4]", the <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> property would be "uint", and the <see cref="GeneratedMavlinkMessageFieldArrayType.ArrayLength"/> property would be 4.
/// </summary>
/// <param name="TypeName">The name of the original Mavlink type.</param>
/// <param name="ConvertedType">The corresponding converted dotnet type for the elements of the array.</param>
/// <param name="ArrayLength">The length of the array.</param>
/// <remarks>
/// <see cref="MavlinkMessageFieldType.TypeName"/> represents the original Mavlink type, while <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> represents the equivalent .NET type for the array's elements.<br/>
/// <see cref="ArrayLength"/> indicates the length of the array.
/// </remarks>
public record GeneratedMavlinkMessageFieldArrayType(string TypeName, string ConvertedType, int ArrayLength) : GeneratedMavlinkMessageFieldType(TypeName, ConvertedType);

/// <summary>
/// Represents a generated Mavlink message field enum type with an additional converted type and a reference to the generated enum.<br/>
/// For a Mavlink enum "ESC_CONNECTION_TYPE" of type "uint8_t", the <see cref="MavlinkMessageFieldType.TypeName"/> property would be "uint8_t", the <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> property would be "byte", and the <see cref="GeneratedMavlinkMessageFieldEnumType.GeneratedEnum"/> property would reference the corresponding generated enum.
/// </summary>
/// <param name="TypeName">The name of the original Mavlink enum type.</param>
/// <param name="ConvertedType">The corresponding converted dotnet type and size for the enum.</param>
/// <param name="GeneratedEnum">The reference to the <see cref="GeneratedMavlinkEnum"/> instance associated with this field.</param>
/// <remarks>
/// <see cref="MavlinkMessageFieldType.TypeName"/> represents the original Mavlink type, while <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> represents the equivalent .NET type and size of the enum.
/// </remarks>
public record GeneratedMavlinkMessageFieldEnumType(string TypeName, string ConvertedType, GeneratedMavlinkEnum GeneratedEnum) : GeneratedMavlinkMessageFieldType(TypeName, ConvertedType);

/// <summary>
/// Represents a generated Mavlink message field array enum type with an additional converted type and a reference to the generated enum.<br/>
/// For a Mavlink enum array "ESC_FAILURE_FLAGS[4]" of type "uint16_t", the <see cref="MavlinkMessageFieldType.TypeName"/> property would be "uint16_t[4]", the <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> property would be "ushort", the <see cref="GeneratedMavlinkMessageFieldArrayEnumType.GeneratedEnum"/> property would reference the corresponding generated enum, and the <see cref="GeneratedMavlinkMessageFieldArrayEnumType.ArrayLength"/> property would be 4.
/// </summary>
/// <param name="TypeName">The name of the original Mavlink type.</param>
/// <param name="ConvertedType">The corresponding converted dotnet type and size for the enum.</param>
/// <param name="GeneratedEnum">The reference to the <see cref="GeneratedMavlinkEnum"/> instance associated with this field.</param>
/// <param name="ArrayLength">The length of the array.</param>
/// <remarks>
/// <see cref="MavlinkMessageFieldType.TypeName"/> represents the original Mavlink type, while <see cref="GeneratedMavlinkMessageFieldType.ConvertedType"/> represents the equivalent .NET type and size of the enum.<br/>
/// <see cref="ArrayLength"/> indicates the length of the array.
/// </remarks>
public record GeneratedMavlinkMessageFieldArrayEnumType(string TypeName, string ConvertedType, GeneratedMavlinkEnum GeneratedEnum, int ArrayLength) : GeneratedMavlinkMessageFieldType(TypeName, ConvertedType);
