namespace Mavlink.Protocol.Generator;

/// <summary>
/// The abstract base record for all generated MAVLink message field types.
/// This hierarchy models the C# representation of a field's type defined in MAVLink XML.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldType(MavlinkMessageFieldType Original)
{
	/// <summary>
	/// Gets the name of the underlying C# type that represents this MAVLink type.
	/// For example, "ushort" for a MAVLink "uint16_t".
	/// </summary>
	public abstract string ConvertedType { get; }
}

/// <summary>
/// Represents a scalar MAVLink field type (a type that holds a single value).
/// This is the base for all non-array types like primitives and enums.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldScalarType(
	string InnerConvertedType,
	MavlinkMessageFieldType Original
) : GeneratedMavlinkMessageFieldType(Original)
{
	/// <inheritdoc/>
	public sealed override string ConvertedType => InnerConvertedType;
}

/// <summary>
/// Represents a MAVLink message field of a primitive type (e.g., uint16_t, float).
/// </summary>
public record GeneratedMavlinkMessageFieldPrimitiveType(
	string InnerConvertedType,
	MavlinkMessageFieldType Original
) : GeneratedMavlinkMessageFieldScalarType(InnerConvertedType, Original);

/// <summary>
/// Represents a MAVLink message field of an enum type.
/// </summary>
public record GeneratedMavlinkMessageFieldEnumType(
	string InnerConvertedType,
	GeneratedMavlinkEnum GeneratedEnum,
	MavlinkMessageFieldType Original
) : GeneratedMavlinkMessageFieldScalarType(InnerConvertedType, Original)
{
	/// <summary>
	/// Gets the original MAVLink type definition, safely cast to its specific enum type.
	/// </summary>
	public MavlinkMessageFieldEnumType SpecificOriginal => (MavlinkMessageFieldEnumType)Original;
}

/// <summary>
/// Represents a MAVLink message field that is an array of a specific scalar type.
/// This record uses composition to hold a reference to the element's type.
/// </summary>
public record GeneratedMavlinkMessageFieldArrayType(
	GeneratedMavlinkMessageFieldScalarType ElementType,
	int ArrayLength,
	MavlinkMessageFieldType Original
) : GeneratedMavlinkMessageFieldType(Original)
{
	/// <inheritdoc/>
	public override string ConvertedType => ElementType.ConvertedType;
}
