using System.Diagnostics.CodeAnalysis;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Parses a MAVLink `invalid` attribute for first-element sentinel rules in arrays.
/// This format invalidates the entire field and is therefore parsed into a
/// <see cref="GeneratedMavlinkMessageWholeFieldValidationRuleDefinition"/>.
/// This parser specifically targets formats ending in `:]`.
/// </summary>
/// <example>
/// `invalid="[NaN:]"`
/// </example>
internal sealed class MavlinkMessageFieldSentinelRuleParser : IMavlinkMessageFieldValidationRuleParser
{
	public bool TryParse(string rawInvalidValue, [NotNullWhen(true)] out GeneratedMavlinkMessageFieldValidationRuleDefinition? definition)
	{
		if (rawInvalidValue.Length >= 3
			&& rawInvalidValue[0] == '['
			&& rawInvalidValue.EndsWith(":]"))
		{
			var token = rawInvalidValue[1..^2].Trim();
			definition = new GeneratedMavlinkMessageWholeFieldValidationRuleDefinition(token);
			return true;
		}

		definition = null;
		return false;
	}
}
