namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink message field that has been generated with an additional generated name.
/// </summary>
/// <remarks>
/// This type inherits from <see cref="MavlinkMessageField"/> and includes an additional property <see cref="GeneratedName"/>.
/// The <see cref="GeneratedName"/> property contains a modified version of the <see cref="MavlinkMessageField.Name"/> property,
/// which reflects the name of the field as it appears in the generated code.
/// </remarks>
public record GeneratedMavlinkMessageField : MavlinkMessageField
{
	/// <summary>
	/// Gets the generated name of the field.
	/// </summary>
	/// <remarks>
	/// The <see cref="GeneratedName"/> represents the field name after it has been processed
	/// and modified for code generation purposes. This might include transformations such as
	/// converting to camelCase, adding prefixes or suffixes, or other modifications to make
	/// the name valid or more readable in the target programming language.
	/// </remarks>
	public string GeneratedName { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageField"/> record.
	/// </summary>
	/// <param name="generatedName">The generated name of the field, derived from the original name.</param>
	/// <param name="type">The type of the field.</param>
	/// <param name="name">The name of the field.</param>
	/// <param name="description">The description of the field.</param>
	/// <param name="display">The display information of the field.</param>
	/// <param name="systemUnit">The system unit of the field.</param>
	/// <param name="isRequired">Indicates whether the field is required.</param>
	/// <param name="printFormat">The print format of the field.</param>
	/// <param name="increment">The increment step for the field value.</param>
	/// <param name="minValue">The minimum value for the field.</param>
	/// <param name="maxValue">The maximum value for the field.</param>
	/// <param name="instance">A value indicating whether the field is instance-specific.</param>
	/// <param name="default">The default value for the field.</param>
	/// <param name="invalid">The invalid value for the field.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="generatedName"/> is <c>null</c>.</exception>
	public GeneratedMavlinkMessageField(
		string generatedName,
		MavlinkMessageFieldType type,
		string name,
		string? description,
		MavlinkMessageFieldDisplay display,
		MavlinkSystemUnit systemUnit,
		bool isRequired,
		string? printFormat,
		float? increment,
		float? minValue,
		float? maxValue,
		bool? instance,
		string? @default,
		string? invalid)
		: base(type, name, description, display, systemUnit, isRequired, printFormat, increment, minValue, maxValue, instance, @default, invalid)
	{
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
	}
}
