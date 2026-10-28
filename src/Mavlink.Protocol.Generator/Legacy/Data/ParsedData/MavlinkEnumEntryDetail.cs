namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents an entry param with various properties.
/// </summary>
public record MavlinkEnumEntryDetail
{
	/// <summary>
	/// The index of the parameter.
	/// The index is a unique identifier for the parameter within a specific context.
	/// </summary>
	public byte Index { get; init; }

	/// <summary>
	/// The label of the parameter.
	/// The label provides a human-readable name for the parameter. It is typically used in user interfaces to display the parameter name.
	/// </summary>
	public string? Label { get; init; }

	/// <summary>
	/// The units of the parameter.
	/// The units specify the measurement units of the parameter value, such as seconds, meters, or degrees.
	/// This helps in understanding the context and scale of the parameter value.
	/// </summary>
	public MavlinkSystemUnit? Units { get; init; }

	/// <summary>
	/// A value indicating whether the parameter is instance-specific.
	/// If <c>true</c>, the parameter is specific to a particular instance of an object.
	/// If <c>false</c> or <c>null</c>, the parameter is general and applicable to all instances.
	/// </summary>
	public bool? Instance { get; init; }

	/// <summary>
	/// The enum name associated with the parameter.
	/// The enum property defines a set of possible values for the parameter, restricting the input to predefined options.
	/// This is useful for parameters that have a limited set of valid values, such as modes or states.
	/// </summary>
	public string? Enum { get; init; }

	/// <summary>
	/// The number of decimal places for the parameter value.
	/// Specifies the precision of the parameter value by indicating the number of decimal places.
	/// This is important for parameters requiring precise values, such as sensor readings or calibration settings.
	/// </summary>
	public byte? DecimalPlaces { get; init; }

	/// <summary>
	/// The increment step for the parameter value.
	/// The increment defines the step size for adjusting the parameter value.
	/// It ensures that the value changes in consistent steps, which is useful for settings that need to be adjusted gradually.
	/// </summary>
	public float? Increment { get; init; }

	/// <summary>
	/// The minimum value for the parameter.
	/// The minimum value sets the lower limit for the parameter.
	/// It helps in validating the parameter value and ensuring it does not go below this limit.
	/// </summary>
	public float? MinValue { get; init; }

	/// <summary>
	/// The maximum value for the parameter.
	/// The maximum value sets the upper limit for the parameter.
	/// It helps in validating the parameter value and ensuring it does not exceed this limit.
	/// </summary>
	public float? MaxValue { get; init; }

	/// <summary>
	/// A value indicating whether the parameter is reserved.
	/// If <c>true</c>, the parameter is reserved for future use or has a special meaning.
	/// Reserved parameters are typically not meant to be modified by users.
	/// </summary>
	public bool? Reserved { get; init; }

	/// <summary>
	/// The default value for the parameter.
	/// The default value is the initial value assigned to the parameter when it is not explicitly set.
	/// This helps in providing a standard starting value for the parameter.
	/// </summary>
	public string? Default { get; init; }

	/// <summary>
	/// The text description for the parameter.
	/// The text description provides additional information or context about the parameter.
	/// It can be used for documentation purposes to explain the purpose and usage of the parameter.
	/// </summary>
	public string[]? Text { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkEnumEntryDetail"/> record.
	/// </summary>
	/// <param name="index">The index of the parameter.</param>
	/// <param name="label">The label of the parameter.</param>
	/// <param name="units">The units of the parameter.</param>
	/// <param name="instance">A value indicating whether the parameter is instance-specific.</param>
	/// <param name="enum">The enum name associated with the parameter.</param>
	/// <param name="decimalPlaces">The number of decimal places for the parameter value.</param>
	/// <param name="increment">The increment step for the parameter value.</param>
	/// <param name="minValue">The minimum value for the parameter.</param>
	/// <param name="maxValue">The maximum value for the parameter.</param>
	/// <param name="reserved">A value indicating whether the parameter is reserved.</param>
	/// <param name="default">The default value for the parameter.</param>
	/// <param name="text">The text description for the parameter.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="index"/> is <c>null</c>.</exception>
	public MavlinkEnumEntryDetail(
		byte index,
		string? label,
		MavlinkSystemUnit? units,
		bool? instance,
		string? @enum,
		byte? decimalPlaces,
		float? increment,
		float? minValue,
		float? maxValue,
		bool? reserved,
		string? @default,
		string[]? text)
	{
		Index = index;
		Label = label;
		Units = units;
		Instance = instance;
		Enum = @enum;
		DecimalPlaces = decimalPlaces;
		Increment = increment;
		MinValue = minValue;
		MaxValue = maxValue;
		Reserved = reserved;
		Default = @default;
		Text = text;
	}
}
