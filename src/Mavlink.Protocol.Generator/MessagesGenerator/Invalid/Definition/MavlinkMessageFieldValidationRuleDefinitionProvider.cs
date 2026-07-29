using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides a concrete implementation for parsing the `invalid` attribute of a MAVLink message field.
/// This provider orchestrates a collection of specialized, order-independent parsers to determine
/// the correct validation rule definition.
/// </summary>
public sealed class MavlinkMessageFieldValidationRuleDefinitionProvider : IMavlinkMessageFieldValidationRuleDefinitionProvider
{
	/// <summary>
	/// A collection of specialized parsers, each responsible for a specific `invalid` attribute format.
	/// The order of parsers in this collection does not matter, as each parser is designed
	/// to strictly match only its designated format.
	/// </summary>
	private readonly ImmutableList<IMavlinkMessageFieldValidationRuleParser> _parsers;

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageFieldValidationRuleDefinitionProvider"/> class
	/// with a default set of rule parsers.
	/// </summary>
	public MavlinkMessageFieldValidationRuleDefinitionProvider()
	{
		_parsers =
		[
			new MavlinkMessageFieldSentinelRuleParser(),      // Recognizes: [VALUE:]
			new MavlinkMessageFieldPerIndexRuleParser(),      // Recognizes: [VALUE1,,VALUE3]
			new MavlinkMessageFieldPerElementRuleParser(),    // Recognizes: [VALUE]
			new MavlinkMessageFieldScalarRuleParser()         // Recognizes: VALUE
		];
	}

	/// <summary>
	/// Gets the validation rule definition for a given MAVLink message field by delegating
	/// to the first parser in the collection that can successfully handle the format.
	/// </summary>
	/// <param name="field">The MAVLink message field from the XML definition.</param>
	/// <returns>A concrete validation rule definition record.</returns>
	/// <exception cref="FormatException">
	/// Thrown if the `invalid` attribute is present but its format cannot be recognized
	/// by any of the registered parsers, indicating a malformed or unsupported value.
	/// </exception>
	public GeneratedMavlinkMessageFieldValidationRuleDefinition GetRuleDefinition(MavlinkMessageField field)
	{
		var rawInvalidValue = field.Invalid?.Trim();
		if (string.IsNullOrWhiteSpace(rawInvalidValue))
		{
			return new GeneratedMavlinkMessageNoValidationRuleDefinition();
		}

		var definition = _parsers
			.Select(parser => parser.TryParse(rawInvalidValue!, out var def) ? def : null)
			.FirstOrDefault(def => def != null);

		return definition ??
			throw new FormatException(
				$"The invalid attribute value '{rawInvalidValue}' for field '{field.Name}' could not be parsed by any known parser."
			);
	}
}
