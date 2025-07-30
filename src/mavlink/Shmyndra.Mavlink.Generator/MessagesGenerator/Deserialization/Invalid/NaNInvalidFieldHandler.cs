namespace Shmyndra.Mavlink.Generator;

public class NaNInvalidFieldHandler : IInvalidFieldHandler
{
	private readonly string _typeName;

	public NaNInvalidFieldHandler(string typeName)
	{
		if (typeName != "float" && typeName != "double")
		{
			throw new ArgumentException("NaN validation is only supported for float and double types.", nameof(typeName));
		}
		_typeName = typeName;
	}

	public string GenerateValidationCondition(string valueExpression)
	{
		return $"!{_typeName}.IsNaN({valueExpression})";
	}

	public string GetInvalidValueExpression()
	{
		return $"{_typeName}.NaN";
	}
}
