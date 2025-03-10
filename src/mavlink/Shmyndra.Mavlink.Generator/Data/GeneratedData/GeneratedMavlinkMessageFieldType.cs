namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Base (non-generic) type for all generated Mavlink message field types.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldTypeBase(string ConvertedType);

/// <summary>
/// Generic base type for generated Mavlink message field types that includes the original Mavlink type.
/// <para>
/// For example, if the Mavlink field is defined as "uint16_t" or with an enum (e.g., enum="ESC_FAILURE_FLAGS"),
/// the original type information is stored in the <paramref name="Original"/> property.
/// </para>
/// </summary>
/// <typeparam name="TOriginal">The original Mavlink message field type (e.g., "uint_t", "enum=...").</typeparam>
public abstract record GeneratedMavlinkMessageFieldType<TOriginal>(string ConvertedType, TOriginal Original)
	: GeneratedMavlinkMessageFieldTypeBase(ConvertedType)
	where TOriginal : MavlinkMessageFieldType;

/// <summary>
/// Represents a generated Mavlink message field for primitive types.
/// Corresponds to Mavlink fields defined as a single value (e.g., "uint_t").
/// </summary>
public record GeneratedMavlinkMessageFieldPrimitiveType(
	string ConvertedType,
	MavlinkMessageFieldType Original)
	: GeneratedMavlinkMessageFieldType<MavlinkMessageFieldType>(ConvertedType, Original);

/// <summary>
/// Represents a generated Mavlink message field for enum types.
/// Corresponds to Mavlink fields defined with an enum specification (e.g., "enum=...").
/// </summary>
public record GeneratedMavlinkMessageFieldEnumType(
	string ConvertedType,
	GeneratedMavlinkEnum GeneratedEnum,
	MavlinkMessageFieldEnumType Original)
	: GeneratedMavlinkMessageFieldType<MavlinkMessageFieldEnumType>(ConvertedType, Original);

/// <summary>
/// Represents a generated Mavlink message field for an array of primitive types.
/// Corresponds to Mavlink fields defined as an array of primitive values (e.g., "uint_t[]").
/// </summary>
public record GeneratedMavlinkMessageFieldArrayType(
	string ConvertedType,
	int ArrayLength,
	MavlinkMessageFieldType Original)
	: GeneratedMavlinkMessageFieldType<MavlinkMessageFieldType>(ConvertedType, Original);

/// <summary>
/// Represents a generated Mavlink message field for an array of enum types.
/// Corresponds to Mavlink fields defined as an array with an enum specification 
/// (e.g., "uint_t[]" where each element is defined as an enum using "enum=...").
/// </summary>
public record GeneratedMavlinkMessageFieldArrayEnumType(
	string ConvertedType,
	GeneratedMavlinkEnum GeneratedEnum,
	int ArrayLength,
	MavlinkMessageFieldEnumType Original)
	: GeneratedMavlinkMessageFieldType<MavlinkMessageFieldEnumType>(ConvertedType, Original);
