namespace Shmyndra.Mavlink.Generator;

public class MaxValueInvalidFieldHandler : IInvalidFieldHandler
{
	private readonly string _maxValue;

	public MaxValueInvalidFieldHandler(string maxValue)
	{
		_maxValue = maxValue;
	}

	public string GenerateValidationCondition(string valueExpression)
	{
		return $"{valueExpression} != {GetInvalidValueExpression()}";
	}

	public string GetInvalidValueExpression()
	{
		return _maxValue.ToUpper() switch
		{
			"UINT8_MAX" => "byte.MaxValue",
			"UINT16_MAX" => "ushort.MaxValue",
			"UINT32_MAX" => "uint.MaxValue",
			"INT8_MAX" => "sbyte.MaxValue",
			"INT16_MAX" => "short.MaxValue",
			"INT32_MAX" => "int.MaxValue",
			_ => throw new NotSupportedException($"Unsupported max value: {_maxValue}")
		};
	}
}
