namespace Shmyndra.Mavlink.Generator;

public class ArrayInvalidFieldHandler : IInvalidFieldHandler
{
	private readonly string _invalidCondition;
	private readonly int _arrayLength;

	public ArrayInvalidFieldHandler(string invalidCondition, int arrayLength)
	{
		_invalidCondition = invalidCondition.Trim('[', ']');
		_arrayLength = arrayLength;
	}

	public string GenerateValidationCondition(string valueExpression)
	{
		if (_invalidCondition.EndsWith(":"))
		{
			string condition = TranslateMaxValue(_invalidCondition.TrimEnd(':'));
			return $"{valueExpression}[0] != {condition}";
		}

		string[] conditions = _invalidCondition.Split(',').Select(c => c.Trim()).ToArray();
		if (conditions.Length > 1)
		{
			var conditionList = new List<string>();
			for (int i = 0; i < conditions.Length && i < _arrayLength; i++)
			{
				string translatedCondition = TranslateMaxValue(conditions[i]);
				conditionList.Add($"{valueExpression}[{i}] != {translatedCondition}");
			}
			return string.Join(" && ", conditionList);
		}

		string singleCondition = TranslateMaxValue(_invalidCondition);
		return $"{valueExpression} != {singleCondition}";
	}

	private string TranslateMaxValue(string value)
	{
		return value.ToUpper() switch
		{
			"UINT8_MAX" => "byte.MaxValue",
			"UINT16_MAX" => "ushort.MaxValue",
			"UINT32_MAX" => "uint.MaxValue",
			"INT8_MAX" => "sbyte.MaxValue",
			"INT16_MAX" => "short.MaxValue",
			"INT32_MAX" => "int.MaxValue",
			_ => value
		};
	}
}
