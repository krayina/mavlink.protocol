namespace Mavlink.Protocol.Generator;

/// <summary>
/// Creates a specific compilation operation based on a given rule definition.
/// This factory acts as a bridge between the data hierarchy and the operation hierarchy.
/// </summary>
public static class MavlinkMessageFieldValidationCompilationFactory
{
	public static MavlinkMessageFieldValidationRuleCompiler CreateOperation(GeneratedMavlinkMessageFieldValidationRuleDefinition definition)
	{
		return definition switch
		{
			GeneratedMavlinkMessageNoValidationRuleDefinition d => new MavlinkMessageFieldNoValidationRuleCompiler(d),
			GeneratedMavlinkMessageWholeFieldValidationRuleDefinition d => new MavlinkMessageFieldWholeFieldRuleCompiler(d),
			GeneratedMavlinkMessagePerElementValidationRuleDefinition d => new MavlinkMessageFieldPerElementRuleCompiler(d),
			GeneratedMavlinkMessagePerIndexValidationRuleDefinition d => new MavlinkMessageFieldPerIndexRuleCompiler(d),
			_ => throw new NotSupportedException($"Rule definition type {definition.GetType().Name} is not supported.")
		};
	}
}
