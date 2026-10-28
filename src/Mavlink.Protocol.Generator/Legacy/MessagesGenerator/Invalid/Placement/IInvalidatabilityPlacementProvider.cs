namespace Mavlink.Protocol.Generator;

/// <summary>
/// Defines a contract for a service that determines the semantic placement of the
/// <c>Invalidatable&lt;T&gt;</c> wrapper based on a MAVLink validation rule definition.
/// </summary>
/// <remarks>
/// This service acts as a bridge between the validation rule parsing stage and the
/// property generation stage. It translates a structured <see cref="GeneratedMavlinkMessageFieldValidationRuleDefinition"/>
/// into a simple, actionable instruction (<see cref="InvalidatabilityPlacement"/>) for the
/// <see cref="MavlinkMessageFieldInitPropertyGenerator"/>.
/// </remarks>
public interface IInvalidatabilityPlacementProvider
{
	/// <summary>
	/// Gets the placement instruction for a given validation rule definition.
	/// </summary>
	/// <param name="definition">The parsed validation rule definition from the MAVLink XML.</param>
	/// <returns>
	/// An object inheriting from <see cref="InvalidatabilityPlacement"/> that describes
	/// where to place the <c>Invalidatable&lt;T&gt;</c> wrapper.
	/// </returns>
	InvalidatabilityPlacement GetPlacement(GeneratedMavlinkMessageFieldValidationRuleDefinition definition);
}
