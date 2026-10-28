namespace Mavlink.Protocol.Generator;

/// <summary>
/// Describes the abstract structure of a generated C# property type.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldTypeStructure;

/// <summary>
/// Represents a scalar type, like "short" or a specific enum name.
/// </summary>
public sealed record GeneratedMavlinkMessageFieldScalarTypeStructure(string TypeName)
	: GeneratedMavlinkMessageFieldTypeStructure;

/// <summary>
/// Represents a type wrapped in an "Invalidatable" decorator.
/// </summary>
public sealed record GeneratedMavlinkMessageFieldInvalidatableTypeStructure(GeneratedMavlinkMessageFieldTypeStructure InnerType)
	: GeneratedMavlinkMessageFieldTypeStructure;

/// <summary>
/// Represents an array type.
/// </summary>
public sealed record GeneratedMavlinkMessageFieldArrayTypeStructure(GeneratedMavlinkMessageFieldTypeStructure ElementType, int Length)
	: GeneratedMavlinkMessageFieldTypeStructure;
