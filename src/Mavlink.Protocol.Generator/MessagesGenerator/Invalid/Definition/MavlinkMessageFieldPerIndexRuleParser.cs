using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Parses a MAVLink `invalid` attribute for per-index array validation rules.
/// This parser specifically targets formats containing brackets and commas.
/// </summary>
/// <example>
/// `invalid="[65535,,65535,]"`
/// </example>
internal sealed class MavlinkMessageFieldPerIndexRuleParser : IMavlinkMessageFieldValidationRuleParser
{
	public bool TryParse(string rawInvalidValue, [NotNullWhen(true)] out GeneratedMavlinkMessageFieldValidationRuleDefinition? definition)
	{
		if (rawInvalidValue.Length >= 3
			&& rawInvalidValue[0] == '['
			&& rawInvalidValue[rawInvalidValue.Length - 1] == ']'
			&& rawInvalidValue.Contains(","))
		{
			var body = rawInvalidValue[1..^1];
			var items = body.Split(',')
							.Select(x => x.Trim())
							.Select(t => string.IsNullOrEmpty(t) ? null : t)
							.ToImmutableArray();

			definition = new GeneratedMavlinkMessagePerIndexValidationRuleDefinition(items);
			return true;
		}

		definition = null;
		return false;
	}
}
