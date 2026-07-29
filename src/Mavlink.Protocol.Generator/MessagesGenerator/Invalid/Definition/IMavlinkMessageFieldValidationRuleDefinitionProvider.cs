namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a contract for a service that parses the `invalid` attribute of a MAVLink message field
/// and translates it into a structured validation rule definition.
/// </summary>
public interface IMavlinkMessageFieldValidationRuleDefinitionProvider
{
	/// <summary>
	/// Gets the validation rule definition for a given MAVLink message field.
	/// </summary>
	/// <param name="field">The MAVLink message field from the XML definition.</param>
	/// <returns>
	/// A specific record inheriting from <see cref="GeneratedMavlinkMessageFieldValidationRuleDefinition"/>
	/// that represents the parsed rule. Returns <see cref="GeneratedMavlinkMessageNoValidationRuleDefinition"/>
	/// if the field has no `invalid` attribute.
	/// </returns>
	GeneratedMavlinkMessageFieldValidationRuleDefinition GetRuleDefinition(MavlinkMessageField field);
}
