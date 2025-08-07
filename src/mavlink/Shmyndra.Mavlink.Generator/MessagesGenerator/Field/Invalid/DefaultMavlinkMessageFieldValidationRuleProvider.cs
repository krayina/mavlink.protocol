namespace Shmyndra.Mavlink.Generator;

public class DefaultMavlinkMessageFieldValidationRuleProvider : IMavlinkMessageFieldValidationRuleProvider
{
	public GeneratedMavlinkMessageFieldValidationRule GetRule(MavlinkMessageField field)
	{
		if (string.IsNullOrWhiteSpace(field.Invalid))
		{
			return new GeneratedMavlinkMessageNoValidationRule();
		}

		bool isPerElementValidation = field.Invalid!.Trim().StartsWith("[")
			&& field.Invalid.Trim().EndsWith("]");

		if (isPerElementValidation)
		{
			return new GeneratedMavlinkMessagePerElementValidationRule(field.Invalid);
		}

		return new GeneratedMavlinkMessageWholeFieldValidationRule(field.Invalid);
	}
}
