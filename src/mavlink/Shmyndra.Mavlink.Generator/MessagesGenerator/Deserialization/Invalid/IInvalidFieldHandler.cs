namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A composite interface for handlers that can provide both a validation condition and an invalid value expression.
/// </summary>
public interface IInvalidFieldHandler : IValidationConditionProvider, IInvalidValueProvider { }

/// <summary>
/// Defines a contract for providing a validation condition for a field.
/// </summary>
public interface IValidationConditionProvider
{
	/// <summary>
	/// Generates a C# condition that checks if the deserialized value is valid.
	/// </summary>
	/// <param name="valueExpression">The expression representing the deserialized value.</param>
	/// <returns>A string representing the validation condition (e.g., "value != 0").</returns>
	string GenerateValidationCondition(string valueExpression);
}

/// <summary>
/// Defines a contract for providing an expression for an "invalid" sentinel value.
/// </summary>
public interface IInvalidValueProvider
{
	/// <summary>
	/// Gets a C# expression for the value that marks a field as invalid or not supplied.
	/// </summary>
	/// <returns>A string representing the C# expression for the invalid value (e.g., "short.MaxValue").</returns>
	string GetInvalidValueExpression();
}
