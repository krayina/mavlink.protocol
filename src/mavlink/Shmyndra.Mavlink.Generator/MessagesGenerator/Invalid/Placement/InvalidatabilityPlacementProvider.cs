namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides a concrete implementation for determining the placement of the
/// <c>Invalidatable&lt;T&gt;</c> wrapper.
/// </summary>
/// <remarks>
/// The logic is based on a direct mapping from the type of the rule definition to a
/// corresponding placement instruction. For example, a rule that applies to the whole field
/// (like a scalar invalid value or an array sentinel) maps to <see cref="WholeFieldInvalidatability"/>.
/// </remarks>
internal class InvalidatabilityPlacementProvider : IInvalidatabilityPlacementProvider
{
	/// <inheritdoc/>
	public InvalidatabilityPlacement GetPlacement(GeneratedMavlinkMessageFieldValidationRuleDefinition definition)
	{
		return definition switch
		{
			GeneratedMavlinkMessageWholeFieldValidationRuleDefinition => new WholeFieldInvalidatability(),
			GeneratedMavlinkMessagePerElementValidationRuleDefinition => new PerElementInvalidatability(),
			GeneratedMavlinkMessagePerIndexValidationRuleDefinition => new PerElementInvalidatability(),

			_ => new NoInvalidatability(),
		};
	}
}

