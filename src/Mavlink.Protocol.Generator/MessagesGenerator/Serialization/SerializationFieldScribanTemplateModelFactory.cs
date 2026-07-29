using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a factory responsible for creating an <see cref="ISerializationFieldScribanTemplateModel"/>
/// for a specific type of MAVLink field.
/// This is the serialization counterpart to <c>IDeserializationFieldScribanTemplateModelFactory</c>.
/// </summary>
internal interface ISerializationFieldScribanTemplateModelFactory
{
	/// <summary>
	/// Determines whether this factory can handle the given MAVLink field.
	/// </summary>
	/// <param name="field">The field to inspect.</param>
	/// <param name="useObjectiveBitmask">The current generator configuration for bitmasks.</param>
	/// <returns><c>true</c> if this factory can create a model for the field; otherwise, <c>false</c>.</returns>
	bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask);

	/// <summary>
	/// Creates a data model for the given field based on the provided context.
	/// </summary>
	/// <param name="context">The context containing all necessary information for model creation.</param>
	/// <returns>An implementation of <see cref="ISerializationFieldScribanTemplateModel"/>.</returns>
	ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context);
}

/// <summary>
/// Factory for char arrays that are treated as null-terminated strings.
/// This factory uses a custom code block because the serialization logic is procedural
/// and best handled entirely by the IPayloadWriteStrategy.
/// </summary>
internal sealed class TerminatedStringSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldArrayType { ConvertedType: "char" };
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.GeneratedType;

		var codeBlock = context.PayloadWriteScribanStrategy.GenerateTerminatedStringWriteBlock(
			$"message.{field.GeneratedName}",
			context.Offset,
			arrayType.ArrayLength
		);

		return new CustomSerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			codeBlock
		);
	}
}

/// <summary>
/// Factory for <see cref="ClassicBitmaskSerializationFieldScribanTemplateModel"/>.
/// Handles enum bitmasks that are serialized from a collection of enum flags.
/// </summary>
internal sealed class ClassicBitmaskSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			   !useObjectiveBitmask;
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;

		// Generate a write statement with a placeholder for the combined value.
		// The Scriban template will be responsible for creating the 'combined' variable.
		var writeStatement = context.PayloadWriteScribanStrategy.GenerateScalarWriteStatement(
			"{value}", // This placeholder will be replaced with the 'combined' variable in the template.
			type.ConvertedType,
			context.Offset
		);

		return new ClassicBitmaskSerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			type.ConvertedType, // The underlying type for bitwise operations (e.g., "ushort").
			writeStatement,
			field.DeclarationSyntax.Type is NullableTypeSyntax
		);
	}
}

/// <summary>
/// Factory for <see cref="ObjectiveBitmaskSerializationFieldScribanTemplateModel"/>.
/// Handles enum bitmasks that are serialized from a dedicated object wrapper.
/// </summary>
internal sealed class ObjectiveBitmaskSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			   useObjectiveBitmask;
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;
		bool isNullable = field.DeclarationSyntax.Type is NullableTypeSyntax;

		// The source of the value is the 'Bitmask' property of the object wrapper.
		string sourceExpression = isNullable
			? $"message.{field.GeneratedName}.Value.Bitmask"
			: $"message.{field.GeneratedName}.Bitmask";

		var writeStatement = context.PayloadWriteScribanStrategy.GenerateScalarWriteStatement(
			sourceExpression,
			type.ConvertedType,
			context.Offset
		);

		return new ObjectiveBitmaskSerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			writeStatement,
			isNullable
		);
	}
}

/// <summary>
/// Factory for <see cref="PrimitiveSerializationFieldScribanTemplateModel"/> for standard (non-bitmask) enum fields.
/// </summary>
internal sealed class EnumFieldSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display != MavlinkMessageFieldDisplay.Bitmask;
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;
		bool isNullable = field.DeclarationSyntax.Type is NullableTypeSyntax;

		// The source value needs to be cast to its underlying primitive type.
		string sourceExpression = isNullable
			? $"({type.ConvertedType})message.{field.GeneratedName}.Value"
			: $"({type.ConvertedType})message.{field.GeneratedName}";

		var writeStatement = context.PayloadWriteScribanStrategy.GenerateScalarWriteStatement(
			sourceExpression,
			type.ConvertedType,
			context.Offset
		);

		return new PrimitiveSerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			writeStatement,
			isNullable
		);
	}
}

/// <summary>
/// Factory for arrays of primitives and enums.
/// </summary>
internal sealed class ArrayFieldSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		// Exclude 'char' arrays, which are handled by the TerminatedString factory.
		return field.GeneratedType is GeneratedMavlinkMessageFieldArrayType arrayType &&
			   arrayType.ConvertedType != "char";
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.GeneratedType;
		var elementType = arrayType.ElementType;

		string sourceExpression = $"message.{field.GeneratedName}[i]";

		if (elementType is GeneratedMavlinkMessageFieldEnumType)
		{
			sourceExpression = $"({elementType.ConvertedType}){sourceExpression}";
		}

		var writeStatement = context.PayloadWriteScribanStrategy.GenerateArrayElementWriteStatement(
			sourceExpression,
			elementType.ConvertedType,
			context.Offset,
			elementType.GetFieldTypeSize(),
			"i"
		);

		bool elementsAreNullable = field.DeclarationSyntax.Type is GenericNameSyntax genericName &&
			genericName.TypeArgumentList.Arguments.FirstOrDefault() is NullableTypeSyntax;

		string? defaultValue = null;
		if (elementsAreNullable)
		{
			if (field.ValidationRule is GeneratedMavlinkMessagePerElementValidationRuleDefinition perElementRule)
			{
				defaultValue = elementType.TranslateToPrimitiveLiteral(perElementRule.RawInvalidValue);
			}
			else if (field.ValidationRule is GeneratedMavlinkMessageWholeFieldValidationRuleDefinition wholeFieldRule)
			{
				defaultValue = elementType.TranslateToPrimitiveLiteral(wholeFieldRule.RawInvalidValue);
			}
		}

		return new ArraySerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			arrayType.ArrayLength.ToString(),
			writeStatement,
			elementsAreNullable,
			defaultValue
		);
	}
}

/// <summary>
/// Factory for <see cref="PrimitiveSerializationFieldScribanTemplateModel"/> for standard primitive types.
/// This should be the last factory in the chain as it's the most generic.
/// </summary>
internal sealed class PrimitiveFieldSerializationFieldScribanTemplateModelFactory : ISerializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldPrimitiveType;
	}

	/// <inheritdoc/>
	public ISerializationFieldScribanTemplateModel CreateModel(FieldSerializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldPrimitiveType)field.GeneratedType;
		bool isNullable = field.DeclarationSyntax.Type is NullableTypeSyntax;

		string sourceExpression = isNullable
			? $"message.{field.GeneratedName}.Value"
			: $"message.{field.GeneratedName}";

		var writeStatement = context.PayloadWriteScribanStrategy.GenerateScalarWriteStatement(
			sourceExpression,
			type.ConvertedType,
			context.Offset
		);

		return new PrimitiveSerializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			writeStatement,
			isNullable
		);
	}
}
