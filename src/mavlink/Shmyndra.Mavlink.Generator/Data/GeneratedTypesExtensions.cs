namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides extension methods for converting Mavlink message fields to their generated counterparts.
/// </summary>
public static class GeneratedTypesExtensions
{
	/// <summary>
	/// Converts a <see cref="MavlinkMessageField"/> to a <see cref="GeneratedMavlinkMessageField"/> with a specified generated name.
	/// </summary>
	/// <param name="field">The original Mavlink message field.</param>
	/// <param name="generatedName">The generated name for the new field.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkMessageField"/> with the specified generated name.</returns>
	public static GeneratedMavlinkMessageField ToGeneratedMavlinkMessageField(this MavlinkMessageField field, string generatedName)
	{
		if (field == null)
		{
			throw new ArgumentNullException(nameof(field));
		}

		return new GeneratedMavlinkMessageField(
			generatedName: generatedName,
			type: field.Type,
			name: field.Name,
			description: field.Description,
			display: field.Display,
			systemUnit: field.SystemUnit,
			isRequired: field.IsRequired,
			printFormat: field.PrintFormat,
			increment: field.Increment,
			minValue: field.MinValue,
			maxValue: field.MaxValue,
			instance: field.Instance,
			@default: field.Default,
			invalid: field.Invalid
		);
	}
}
