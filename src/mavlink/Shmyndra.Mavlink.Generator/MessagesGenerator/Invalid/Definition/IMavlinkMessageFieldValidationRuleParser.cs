using System.Diagnostics.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a contract for a parser that attempts to create a specific
/// <see cref="GeneratedMavlinkMessageFieldValidationRuleDefinition"/> from the raw string
/// value of a MAVLink message field's `invalid` attribute.
/// </summary>
internal interface IMavlinkMessageFieldValidationRuleParser
{
	/// <summary>
	/// Tries to parse the raw `invalid` attribute value. Each implementation is responsible
	/// for strictly identifying and parsing one specific format.
	/// </summary>
	/// <param name="rawInvalidValue">The raw string value from the `invalid` attribute.</param>
	/// <param name="definition">
	/// When this method returns true, contains the resulting rule definition; otherwise, null.
	/// </param>
	/// <returns>
	/// <c>true</c> if the parser recognized the format and successfully created a definition;
	/// otherwise, <c>false</c>.
	/// </returns>
	bool TryParse(string rawInvalidValue, [NotNullWhen(true)] out GeneratedMavlinkMessageFieldValidationRuleDefinition? definition);
}
