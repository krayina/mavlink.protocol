using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents the data model for a single field being deserialized.
/// This serves as a "view-model" for the Scriban template, containing all necessary
/// information to generate the deserialization code for one field.
/// </summary>
internal interface IDeserializationFieldScribanTemplateModel
{
	/// <summary>
	/// Gets the name of the property in the final message class (e.g., "Altitude").
	/// </summary>
	string PropertyName { get; }

	/// <summary>
	/// Gets the name of the local variable that will hold the deserialized value (e.g., "altitude").
	/// </summary>
	string VariableName { get; }

	/// <summary>
	/// Gets a unique name for the template partial to use for rendering this model.
	/// This allows the main Scriban template to select the correct rendering logic.
	/// </summary>
	string TemplateName { get; }
}

/// <summary>
/// A model representing a primitive type field (e.g., int, float, uint8_t).
/// </summary>
internal sealed record PrimitiveDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string ResultTypeName,
	string ReadExpression,
	string? ValidationCondition = null
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "PrimitiveField";
}

/// <summary>
/// A model representing a standard enum field.
/// </summary>
internal sealed record EnumDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string ResultTypeName,
	string ReadExpression,
	string? ValidationCondition = null
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "EnumField";
}

/// <summary>
/// A model representing a bitmask field that should be deserialized as a collection of enum flags.
/// </summary>
internal sealed record ClassicBitmaskDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string EnumTypeName,
	string ReadExpression,
	string BitwiseOperationType,
	int TotalBits
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "ClassicBitmaskField";
}

/// <summary>
/// A model representing a bitmask field that should be deserialized into a dedicated object wrapper.
/// </summary>
internal sealed record ObjectiveBitmaskDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string BitmaskTypeName,
	string readExpression
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "ObjectiveBitmaskField";
}

public record ArrayDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string TempArrayInitialization,
	string ElementReadExpression,
	string ArrayLength,
	string ElementFinalType,
	ImmutableDictionary<int, string> PerIndexValidation,
	string? AllElementsValidation
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "ArrayField";
	public bool HasValidation => PerIndexValidation.Count > 0 || AllElementsValidation != null;
}

/// <summary>
/// A model representing a field whose deserialization logic is complex and
/// is fully generated as a code block in the factory.
/// </summary>
public record CustomDeserializationFieldScribanTemplateModel(
	string PropertyName,
	string VariableName,
	string DeserializationCode
) : IDeserializationFieldScribanTemplateModel
{
	public string TemplateName => "CustomDeserializationCode";
}
