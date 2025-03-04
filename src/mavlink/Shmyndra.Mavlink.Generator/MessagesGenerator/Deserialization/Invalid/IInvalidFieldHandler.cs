namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a contract for generating validation conditions for deserialized fields.
/// </summary>
public interface IInvalidFieldHandler
{
	/// <summary>
	/// Generates a condition that checks if the deserialized value is valid.
	/// </summary>
	/// <param name="valueExpression">The expression representing the deserialized value.</param>
	/// <returns>A string representing the validation condition (e.g., "value != 0").</returns>
	string GenerateValidationCondition(string valueExpression);
}
