namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents the data model for a single field being serialized.
/// This serves as a "view-model" for the Scriban template, containing all necessary
/// information to generate the serialization code for one field.
/// This is the serialization counterpart to <c>IDeserializationFieldScribanTemplateModel</c>.
/// </summary>
internal interface ISerializationFieldScribanTemplateModel
{
	/// <summary>
	/// Gets the name of the property in the message class (e.g., "Altitude").
	/// </summary>
	string PropertyName { get; }

	/// <summary>
	/// Gets a unique name for the template partial to use for rendering this model.
	/// This allows the main Scriban template to select the correct rendering logic.
	/// </summary>
	string TemplateName { get; }
}

/// <summary>
/// A model for a primitive type or standard enum field (e.g., int, float, non-bitmask enum).
/// </summary>
internal sealed record PrimitiveSerializationFieldScribanTemplateModel(
	string PropertyName,
	string WriteStatement,
	bool IsNullable
) : ISerializationFieldScribanTemplateModel
{
	public string TemplateName => "PrimitiveField";
}

/// <summary>
/// A model representing a bitmask field that is stored as a collection of enum flags.
/// </summary>
internal sealed record ClassicBitmaskSerializationFieldScribanTemplateModel(
	string PropertyName,
	string UnderlyingType,
	string WriteStatement, // Contains a placeholder {value} to be replaced with the combined flags variable.
	bool IsNullable
) : ISerializationFieldScribanTemplateModel
{
	public string TemplateName => "ClassicBitmaskField";
}

/// <summary>
/// A model representing a bitmask field that is stored in a dedicated object wrapper.
/// </summary>
internal sealed record ObjectiveBitmaskSerializationFieldScribanTemplateModel(
	string PropertyName,
	string WriteStatement,
	bool IsNullable
) : ISerializationFieldScribanTemplateModel
{
	public string TemplateName => "ObjectiveBitmaskField";
}

/// <summary>
/// A model representing an array field (of primitives or enums).
/// </summary>
internal sealed record ArraySerializationFieldScribanTemplateModel(
	string PropertyName,
	string ArrayLength,
	string ElementWriteStatement, // The statement to write one element, containing index and source placeholders.
	bool ElementsAreNullable,
	string? DefaultValueExpression // The C# literal for the 'invalid' value (e.g., "ushort.MaxValue"), used if an element is null.
) : ISerializationFieldScribanTemplateModel
{
	public string TemplateName => "ArrayField";
}

/// <summary>
/// A model representing a field whose serialization logic is complex and
/// is fully generated as a code block (e.g., for null-terminated strings).
/// </summary>
internal sealed record CustomSerializationFieldScribanTemplateModel(
	string PropertyName,
	string SerializationCodeBlock
) : ISerializationFieldScribanTemplateModel
{
	public string TemplateName => "CustomSerializationCode";
}
