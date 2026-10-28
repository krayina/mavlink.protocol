namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents a field in a Mavlink message with various properties.
/// </summary>
public record MavlinkMessageField
{
	/// <summary>
	/// The type of the field.
	/// </summary>
	public MavlinkMessageFieldType Type { get; init; }

	/// <summary>
	/// The name of the field.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// The description of the field.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// The display information of the field.
	/// </summary>
	public MavlinkMessageFieldDisplay Display { get; init; }

	/// <summary>
	/// The system unit of the field.
	/// </summary>
	public MavlinkSystemUnit SystemUnit { get; init; }

	/// <summary>
	/// Gets a value indicating whether this property is required.
	/// This property is nullable and is not mandatory for byte deserialization.
	/// </summary>
	public bool IsRequired { get; init; }

	/// <summary>
	/// The print format of the field.
	/// The print format specifies how the value of the field should be formatted when displayed or printed.
	/// </summary>
	public string? PrintFormat { get; init; }

	/// <summary>
	/// The increment step for the field value.
	/// The increment defines the step size for adjusting the field value.
	/// It ensures that the value changes in consistent steps, which is useful for settings that need to be adjusted gradually.
	/// </summary>
	public float? Increment { get; init; }

	/// <summary>
	/// The minimum value for the field.
	/// The minimum value sets the lower limit for the field.
	/// It helps in validating the field value and ensuring it does not go below this limit.
	/// </summary>
	public float? MinValue { get; init; }

	/// <summary>
	/// The maximum value for the field.
	/// The maximum value sets the upper limit for the field.
	/// It helps in validating the field value and ensuring it does not exceed this limit.
	/// </summary>
	public float? MaxValue { get; init; }

	/// <summary>
	/// A value indicating whether the field is instance-specific.
	/// If <c>true</c>, the field is specific to a particular instance of an object.
	/// If <c>false</c> or <c>null</c>, the field is general and applicable to all instances.
	/// </summary>
	public bool? Instance { get; init; }

	/// <summary>
	/// The default value for the field.
	/// The default value is the initial value assigned to the field when it is not explicitly set.
	/// This helps in providing a standard starting value for the field.
	/// </summary>
	public string? Default { get; init; }

	/// <summary>
	/// The invalid value for the field.
	/// The invalid value specifies a value that indicates an error or an out-of-range condition.
	/// This helps in identifying and handling invalid data.
	/// </summary>
	public string? Invalid { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageField"/> record.
	/// </summary>
	/// <param name="type">The type of the field.</param>
	/// <param name="name">The name of the field.</param>
	/// <param name="description">The description of the field.</param>
	/// <param name="display">The display information of the field.</param>
	/// <param name="systemUnit">The system unit of the field.</param>
	/// <param name="printFormat">The print format of the field.</param>
	/// <param name="increment">The increment step for the field value.</param>
	/// <param name="minValue">The minimum value for the field.</param>
	/// <param name="maxValue">The maximum value for the field.</param>
	/// <param name="instance">A value indicating whether the field is instance-specific.</param>
	/// <param name="default">The default value for the field.</param>
	/// <param name="invalid">The invalid value for the field.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <c>null</c>.</exception>
	public MavlinkMessageField(
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
	{
		Type = type;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Description = description;
		Display = display;
		SystemUnit = systemUnit;
		IsRequired = isRequired;
		PrintFormat = printFormat;
		Increment = increment;
		MinValue = minValue;
		MaxValue = maxValue;
		Instance = instance;
		Default = @default;
		Invalid = invalid;
	}
}
