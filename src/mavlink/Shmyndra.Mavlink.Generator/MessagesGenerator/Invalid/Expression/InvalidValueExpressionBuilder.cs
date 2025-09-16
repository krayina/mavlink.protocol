using System.Globalization;

namespace Shmyndra.Mavlink.Generator;

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

		string literal = TranslateToPrimitiveLiteral(rawInvalidValue, type);

		if (type is GeneratedMavlinkMessageFieldEnumType enumType)
		{
			return $"({enumType.ConvertedType}){variableName} != {literal}";
		}

		return $"{variableName} != {literal}";
	}

	private string TranslateToPrimitiveLiteral(string rawInvalidValue, GeneratedMavlinkMessageFieldType type)
	{
		switch (rawInvalidValue.ToUpperInvariant())
		{
			case "UINT8_MAX": return "byte.MaxValue";
			case "UINT16_MAX": return "ushort.MaxValue";
			case "UINT32_MAX": return "uint.MaxValue";
			case "UINT64_MAX": return "ulong.MaxValue";
			case "INT8_MAX": return "sbyte.MaxValue";
			case "INT16_MAX": return "short.MaxValue";
			case "INT32_MAX": return "int.MaxValue";
			case "INT64_MAX": return "long.MaxValue";
		}

		if (double.TryParse(rawInvalidValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
		{
			string literal;
			if (type.ConvertedType == "float")
			{
				literal = ((float)parsedValue).ToString("R", CultureInfo.InvariantCulture) + "f";
			}
			else
			{
				literal = parsedValue.ToString("R", CultureInfo.InvariantCulture);
			}

			return literal;
		}

		throw new FormatException(
			$"The raw invalid value '{rawInvalidValue}' is not a valid numeric literal or a known MAVLink constant."
		);
	}
}
