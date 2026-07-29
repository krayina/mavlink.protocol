using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents an abstract compilation operation that transforms a rule definition
/// into a C# validation expression. This is the base for all specific
/// compilation operation types.
/// </summary>
public abstract record MavlinkMessageFieldValidationRuleCompiler
{
	/// <summary>
	/// Executes the compilation logic for the specific rule.
	/// </summary>
	/// <param name="context">Provides contextual information needed for compilation, 
	/// such as the field type and an expression builder.</param>
	/// <returns>A structured C# validation expression.</returns>
	public abstract GeneratedMavlinkMessageFieldValidationExpression Compile(MavlinkValidationCompilationContext context);
}

/// <summary>
/// An operation to handle the absence of a validation rule. It consistently
/// produces a <see cref="GeneratedMavlinkMessageFieldNoValidationExpression"/>.
/// </summary>
internal sealed record MavlinkMessageFieldNoValidationRuleCompiler(GeneratedMavlinkMessageNoValidationRuleDefinition Definition)
	: MavlinkMessageFieldValidationRuleCompiler
{
	public override GeneratedMavlinkMessageFieldValidationExpression Compile(MavlinkValidationCompilationContext context)
	{
		return new GeneratedMavlinkMessageFieldNoValidationExpression();
	}
}

/// <summary>
/// An operation to compile a whole-field validation rule. It generates a single
/// C# condition that applies to the entire field value.
/// </summary>
internal sealed record MavlinkMessageFieldWholeFieldRuleCompiler(GeneratedMavlinkMessageWholeFieldValidationRuleDefinition Definition)
	: MavlinkMessageFieldValidationRuleCompiler
{
	public override GeneratedMavlinkMessageFieldValidationExpression Compile(MavlinkValidationCompilationContext context)
	{
		string condition = context.ExpressionBuilder.BuildCondition(
			variableName: "value",
			rawInvalidValue: Definition.RawInvalidValue,
			type: context.FieldType);

		return new GeneratedMavlinkMessageFieldWholeValidationExpression(condition);
	}
}

/// <summary>
/// An operation to compile a per-element validation rule for arrays. It produces
/// a C# condition template to be used inside a loop for each array element.
/// </summary>
internal sealed record MavlinkMessageFieldPerElementRuleCompiler(GeneratedMavlinkMessagePerElementValidationRuleDefinition Definition)
	: MavlinkMessageFieldValidationRuleCompiler
{
	public override GeneratedMavlinkMessageFieldValidationExpression Compile(MavlinkValidationCompilationContext context)
	{
		string elementConditionTemplate = context.ExpressionBuilder.BuildCondition(
			variableName: "{element}",
			rawInvalidValue: Definition.RawInvalidValue,
			type: context.FieldType.GetElementTypeOrSelf());

		return new GeneratedMavlinkMessageFieldPerElementValidationExpression(elementConditionTemplate);
	}
}

/// <summary>
/// An operation to compile a per-index validation rule. It generates a map of
/// validation conditions, where each condition is tied to a specific array index.
/// </summary>
internal sealed record MavlinkMessageFieldPerIndexRuleCompiler(GeneratedMavlinkMessagePerIndexValidationRuleDefinition Definition)
	: MavlinkMessageFieldValidationRuleCompiler
{
	public override GeneratedMavlinkMessageFieldValidationExpression Compile(MavlinkValidationCompilationContext context)
	{
		var conditionsByIndex = ImmutableDictionary.CreateBuilder<int, string>();
		var elementType = context.FieldType.GetElementTypeOrSelf();

		for (int i = 0; i < Definition.InvalidValuesInOrder.Length; i++)
		{
			string? rawInvalidValue = Definition.InvalidValuesInOrder[i];

			if (!string.IsNullOrEmpty(rawInvalidValue))
			{
				string condition = context.ExpressionBuilder.BuildCondition(
					variableName: "element",
					rawInvalidValue: rawInvalidValue!,
					type: elementType);

				conditionsByIndex.Add(i, condition);
			}
		}

		return new GeneratedMavlinkMessageFieldPerIndexValidationExpression(conditionsByIndex.ToImmutable());
	}
}
