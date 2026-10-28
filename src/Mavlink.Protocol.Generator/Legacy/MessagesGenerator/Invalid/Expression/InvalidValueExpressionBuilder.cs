namespace Mavlink.Protocol.Generator;

/// <summary>
/// A concrete implementation of the expression builder.
/// </summary>
public class InvalidValueExpressionBuilder : IInvalidValueExpressionBuilder
{
	public string BuildCondition(string variableName, string rawInvalidValue, GeneratedMavlinkMessageFieldType type)
	{
		if (string.Equals(rawInvalidValue, "NaN", StringComparison.OrdinalIgnoreCase))
		{
			switch (type.ConvertedType)
			{
				case "float":
					return $"!float.IsNaN({variableName})";
				case "double":
					return $"!double.IsNaN({variableName})";
				default:
					throw new FormatException(
						$"The invalid value 'NaN' is only applicable to floating-point types, but was used with '{type.ConvertedType}'.");
			}
		}

		string literal = type.TranslateToPrimitiveLiteral(rawInvalidValue);

		if (type is GeneratedMavlinkMessageFieldEnumType enumType)
		{
			return $"({enumType.ConvertedType}){variableName} != {literal}";
		}

		return $"{variableName} != {literal}";
	}
}
