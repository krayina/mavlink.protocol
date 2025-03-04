namespace Shmyndra.Mavlink.Generator;

public class EnumInvalidFieldHandler : IInvalidFieldHandler
{
	private readonly string _invalidValue;
	private readonly string _enumTypeName;
	private readonly string _underlyingType;

	public EnumInvalidFieldHandler(string invalidValue, string enumTypeName, string underlyingType)
	{
		_invalidValue = invalidValue;
		_enumTypeName = enumTypeName;
		_underlyingType = underlyingType;
	}

	public string GenerateValidationCondition(string valueExpression)
	{
		if (_invalidValue.Contains("MAX"))
		{
			return _invalidValue switch
			{
				"UINT8_MAX" => $"{valueExpression} != byte.MaxValue",
				"UINT16_MAX" => $"{valueExpression} != ushort.MaxValue",
				"UINT32_MAX" => $"{valueExpression} != uint.MaxValue",
				_ => throw new NotSupportedException($"Unsupported max value for enum: {_invalidValue}")
			};
		}
		else if (_invalidValue.StartsWith(_enumTypeName))
		{
			return $"{valueExpression} != ({_underlyingType}){_enumTypeName}.{_invalidValue.Split('.').Last()}";
		}
		return $"{valueExpression} != {_invalidValue}";
	}
}
