namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a strategy for determining the validation rule for a MAVLink field.
/// </summary>
public interface IMavlinkMessageFieldValidationRuleProvider
{
	GeneratedMavlinkMessageFieldValidationRule GetRule(MavlinkMessageField field);
}
