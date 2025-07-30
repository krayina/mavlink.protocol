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
		return $"{valueExpression} != ({_underlyingType}){GetInvalidValueExpression()}";
	}

	public string GetInvalidValueExpression()
	{
		if (int.TryParse(_invalidValue, out _))
		{
			return _invalidValue;
		}

		if (_invalidValue.StartsWith(_enumTypeName))
		{
			return $"{_enumTypeName}.{_invalidValue.Split('.').Last()}";
		}

		if (_invalidValue.Contains("MAX"))
		{
			return _invalidValue switch
			{
				"UINT8_MAX" => "byte.MaxValue",
				"UINT16_MAX" => "ushort.MaxValue",
				"UINT32_MAX" => "uint.MaxValue",
				_ => throw new NotSupportedException($"Unsupported max value for enum: {_invalidValue}")
			};
		}

		throw new NotSupportedException($"Cannot determine invalid value expression for enum from '{_invalidValue}'");
	}
}
