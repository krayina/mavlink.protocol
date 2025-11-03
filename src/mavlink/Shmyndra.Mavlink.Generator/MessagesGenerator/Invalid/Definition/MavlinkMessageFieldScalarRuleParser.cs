using System.Diagnostics.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Parses a MAVLink `invalid` attribute for scalar fields or whole-field validation.
/// This parser specifically targets formats that are not enclosed in brackets.
/// </summary>
/// <example>
/// `invalid="-1"`
/// </example>
internal sealed class MavlinkMessageFieldScalarRuleParser : IMavlinkMessageFieldValidationRuleParser
{
	public bool TryParse(string rawInvalidValue, [NotNullWhen(true)] out GeneratedMavlinkMessageFieldValidationRuleDefinition? definition)
	{
		if (rawInvalidValue.Length > 0
			&& rawInvalidValue[0] != '[')
		{
			definition = new GeneratedMavlinkMessageWholeFieldValidationRuleDefinition(rawInvalidValue);
			return true;
		}

		definition = null;
		return false;
	}
}
