using System.Diagnostics.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Parses a MAVLink `invalid` attribute for per-element array validation rules.
/// This parser specifically targets formats with brackets but without commas or a sentinel marker.
/// </summary>
/// <example>
/// `invalid="[NaN]"`
/// </example>
internal sealed class MavlinkMessageFieldPerElementRuleParser : IMavlinkMessageFieldValidationRuleParser
{
	public bool TryParse(string rawInvalidValue, [NotNullWhen(true)] out GeneratedMavlinkMessageFieldValidationRuleDefinition? definition)
	{
		if (rawInvalidValue.Length >= 2
			&& rawInvalidValue[0] == '['
			&& rawInvalidValue[rawInvalidValue.Length - 1] == ']'
			&& !rawInvalidValue.EndsWith(":]")
			&& !rawInvalidValue.Contains(","))
		{
			var body = rawInvalidValue[1..^1].Trim();
			definition = new GeneratedMavlinkMessagePerElementValidationRuleDefinition(body);
			return true;
		}

		definition = null;
		return false;
	}
}
