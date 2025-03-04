namespace Shmyndra.Mavlink.Generator;

public class LiteralInvalidFieldHandler : IInvalidFieldHandler
{
	private readonly string _invalidLiteral;

	public LiteralInvalidFieldHandler(string invalidLiteral)
	{
		_invalidLiteral = invalidLiteral;
	}

	public string GenerateValidationCondition(string valueExpression)
	{
		return $"{valueExpression} != {_invalidLiteral}";
	}
}
