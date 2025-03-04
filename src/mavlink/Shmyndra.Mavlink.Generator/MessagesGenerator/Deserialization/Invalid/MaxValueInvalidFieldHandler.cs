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
		return _maxValue switch
		{
			"UINT8_MAX" => $"{valueExpression} != byte.MaxValue",
			"UINT16_MAX" => $"{valueExpression} != ushort.MaxValue",
			"UINT32_MAX" => $"{valueExpression} != uint.MaxValue",
			"INT8_MAX" => $"{valueExpression} != sbyte.MaxValue",
			"INT16_MAX" => $"{valueExpression} != short.MaxValue",
			"INT32_MAX" => $"{valueExpression} != int.MaxValue",
			_ => throw new NotSupportedException($"Unsupported max value: {_maxValue}")
		};
	}
}
