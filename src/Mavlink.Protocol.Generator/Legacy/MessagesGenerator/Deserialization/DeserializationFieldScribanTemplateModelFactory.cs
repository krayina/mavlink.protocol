using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Defines a factory responsible for creating an <see cref="IDeserializationFieldScribanTemplateModel"/>
/// for a specific type of MAVLink field.
/// </summary>
internal interface IDeserializationFieldScribanTemplateModelFactory
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
	/// <returns>An implementation of <see cref="IDeserializationFieldScribanTemplateModel"/>.</returns>
	IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context);
}

/// <summary>
/// Factory for char arrays that are treated as null-terminated strings.
/// This factory is a pragmatic exception that uses a custom code block because the deserialization
/// logic is procedural and highly platform-dependent, making it a perfect candidate
/// for the IPayloadReadStrategy to handle completely.
/// </summary>
internal sealed class TerminatedStringDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldArrayType { ConvertedType: "char" };
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.GeneratedType;
		var variableName = Utilities.ToLowerCamelCase(field.GeneratedName);

		var codeBlock = context.PayloadReadScribanStrategy.GenerateTerminatedStringReadBlock(
			variableName,
			context.Offset,
			arrayType.ArrayLength
		);

		return new CustomDeserializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			variableName,
			codeBlock
		);
	}
}

/// <summary>
/// Factory for <see cref="ClassicBitmaskDeserializationFieldScribanTemplateModel"/>. Handles enum bitmasks for standard deserialization.
/// </summary>
internal sealed class ClassicBitmaskDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			   !useObjectiveBitmask;
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;
		int size = type.GetFieldTypeSize();

		return new ClassicBitmaskDeserializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			Utilities.ToLowerCamelCase(field.GeneratedName),
			type.GetQualifiedEnumTypeName(context.CurrentNamespace),
			context.PayloadReadScribanStrategy.GenerateScalarReadExpression(type.ConvertedType, context.Offset, size),
			Utilities.GetCombinedTypeForTotalBits(size * 8),
			size * 8
		);
	}
}

/// <summary>
/// Factory for <see cref="ObjectiveBitmaskDeserializationFieldScribanTemplateModel"/>. Handles enum bitmasks for objective-style deserialization.
/// </summary>
internal sealed class ObjectiveBitmaskDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			   useObjectiveBitmask;
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;
		int size = type.GetFieldTypeSize();

		return new ObjectiveBitmaskDeserializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			Utilities.ToLowerCamelCase(field.GeneratedName),
			$"{type.GetQualifiedEnumTypeName(context.CurrentNamespace)}Bitmask",
			context.PayloadReadScribanStrategy.GenerateScalarReadExpression(type.ConvertedType, context.Offset, size)
		);
	}
}

/// <summary>
/// Factory for <see cref="EnumDeserializationFieldScribanTemplateModel"/>. Handles standard (non-bitmask) enum fields.
/// </summary>
internal sealed class EnumFieldDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldEnumType &&
			   field.Original.Display != MavlinkMessageFieldDisplay.Bitmask;
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldEnumType)field.GeneratedType;
		int size = type.GetFieldTypeSize();
		var enumTypeName = type.GetQualifiedEnumTypeName(context.CurrentNamespace);

		var readExpression = context.PayloadReadScribanStrategy.GenerateScalarReadExpression(
			type.ConvertedType,
			context.Offset,
			size);

		string? validationCondition = null;
		var resultTypeName = enumTypeName;

		if (field.ValidationRule is not GeneratedMavlinkMessageNoValidationRuleDefinition)
		{
			var validationExpression = context.ValidationCompiler.Compile(field.ValidationRule, type);
			if (validationExpression is GeneratedMavlinkMessageFieldWholeValidationExpression whole)
			{
				validationCondition = whole.ConditionForWholeField;
				resultTypeName = $"{enumTypeName}?";
			}
		}

		var model = new EnumDeserializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			Utilities.ToLowerCamelCase(field.GeneratedName),
			resultTypeName,
			readExpression)
		{
			ValidationCondition = validationCondition
		};
		return model;
	}
}

/// <summary>
/// Factory for <see cref="PrimitiveDeserializationFieldScribanTemplateModel"/>. Handles standard primitive types like int, float, etc.
/// </summary>
internal sealed class PrimitiveFieldDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldPrimitiveType;
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var type = (GeneratedMavlinkMessageFieldPrimitiveType)field.GeneratedType;
		var originalTypeName = type.ConvertedType;
		int size = type.GetFieldTypeSize();

		var readExpression = context.PayloadReadScribanStrategy.GenerateScalarReadExpression(
			originalTypeName,
			context.Offset,
			size);

		string? validationCondition = null;
		var resultTypeName = originalTypeName;

		if (field.ValidationRule is not GeneratedMavlinkMessageNoValidationRuleDefinition)
		{
			var validationExpression = context.ValidationCompiler.Compile(field.ValidationRule, type);
			if (validationExpression is GeneratedMavlinkMessageFieldWholeValidationExpression whole)
			{
				validationCondition = whole.ConditionForWholeField;
				resultTypeName = $"{originalTypeName}?";
			}
			else
			{
				throw new NotSupportedException("Only 'WholeField' validation is supported for primitive types.");
			}
		}

		var model = new PrimitiveDeserializationFieldScribanTemplateModel(
			Utilities.EscapeReservedKeyword(field.GeneratedName),
			Utilities.ToLowerCamelCase(field.GeneratedName),
			resultTypeName,
			readExpression)
		{
			ValidationCondition = validationCondition
		};
		return model;
	}
}

/// <summary>
/// Factory for arrays of primitives and enums. Creates a rich, declarative model
/// for the template to render, instead of generating code itself.
/// </summary>
internal sealed class ArrayFieldDeserializationFieldScribanTemplateModelFactory : IDeserializationFieldScribanTemplateModelFactory
{
	/// <inheritdoc/>
	public bool CanHandle(GeneratedMavlinkMessageField field, bool useObjectiveBitmask)
	{
		return field.GeneratedType is GeneratedMavlinkMessageFieldArrayType arrayType &&
			   arrayType.ConvertedType != "char";
	}

	/// <inheritdoc/>
	public IDeserializationFieldScribanTemplateModel CreateModel(FieldDeserializationScribanContext context)
	{
		var field = context.Field;
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.GeneratedType;
		var elementType = arrayType.ElementType;
		var variableName = Utilities.ToLowerCamelCase(field.GeneratedName);

		string elementResultTypeName = elementType is GeneratedMavlinkMessageFieldEnumType enumType
			? enumType.GetQualifiedEnumTypeName(context.CurrentNamespace)
			: elementType.ConvertedType;

		string tempElementType = field.ValidationRule is GeneratedMavlinkMessageNoValidationRuleDefinition
			? elementResultTypeName
			: $"{elementResultTypeName}?";

		var tempArrayInit = $"var {variableName}Temp = new {tempElementType}[{arrayType.ArrayLength}];";

		var elementReadExpr = context.PayloadReadScribanStrategy.GenerateArrayElementReadExpression(
			elementType.ConvertedType, context.Offset, elementType.GetFieldTypeSize(), "i");

		var perIndexValidation = ImmutableDictionary<int, string>.Empty;
		string? allElementsValidation = null;
		if (context.ValidationCompiler.Compile(field.ValidationRule, arrayType) is var validation)
		{
			if (validation is GeneratedMavlinkMessageFieldPerIndexValidationExpression perIndex)
			{
				perIndexValidation = perIndex.ConditionByIndex.ToImmutableDictionary(
					kvp => kvp.Key,
					kvp => kvp.Value.Replace("element", "value"));
			}
			else if (validation is GeneratedMavlinkMessageFieldWholeValidationExpression whole)
			{
				allElementsValidation = whole.ConditionForWholeField.Replace("value", "value");
			}
		}

		return new ArrayDeserializationFieldScribanTemplateModel(
			PropertyName: Utilities.EscapeReservedKeyword(field.GeneratedName),
			VariableName: variableName,
			TempArrayInitialization: tempArrayInit,
			ElementReadExpression: elementReadExpr,
			ArrayLength: arrayType.ArrayLength.ToString(),
			ElementFinalType: elementResultTypeName,
			PerIndexValidation: perIndexValidation,
			AllElementsValidation: allElementsValidation
		);
	}
}
