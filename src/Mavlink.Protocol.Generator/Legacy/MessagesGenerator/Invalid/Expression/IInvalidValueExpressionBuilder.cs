namespace Mavlink.Protocol.Generator;

/// <summary>
/// Defines a service responsible for building a C# boolean validity condition
/// from a raw string value found in the MAVLink XML `invalid` attribute.
/// </summary>
public interface IInvalidValueExpressionBuilder
{
	/// <summary>
	/// Builds a C# boolean expression that evaluates to `true` if the value is valid.
	/// </summary>
	/// <param name="variableName">The name of the C# variable to check (e.g., "value", "{element}").</param>
	/// <param name="rawInvalidValue">The raw string from the `invalid` attribute (e.g., "NaN", "-1", "UINT32_MAX").</param>
	/// <param name="type">The C# type information of the field, used to generate type-specific checks (like `float.IsNaN`).</param>
	/// <returns>A C# string representing the validity condition (e.g., "!float.IsNaN(value)", "value != 4294967295").</returns>
	string BuildCondition(string variableName, string rawInvalidValue, GeneratedMavlinkMessageFieldType type);
}
