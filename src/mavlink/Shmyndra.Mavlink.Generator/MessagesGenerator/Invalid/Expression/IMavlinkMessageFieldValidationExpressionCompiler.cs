namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a contract for a service that "compiles" a semantic validation rule definition
/// into a concrete, C#-specific validation expression.
/// </summary>
public interface IMavlinkMessageFieldValidationExpressionCompiler
{
	/// <summary>
	/// Compiles a rule definition into a validation expression.
	/// </summary>
	/// <param name="definition">The abstract rule definition parsed from MAVLink XML.</param>
	/// <param name="fieldType">The C# type information for the field, needed to generate a correct expression.</param>
	/// <returns>A concrete validation expression record containing ready-to-use C# code snippets.</returns>
	GeneratedMavlinkMessageFieldValidationExpression Compile(
		GeneratedMavlinkMessageFieldValidationRuleDefinition definition,
		GeneratedMavlinkMessageFieldType fieldType);
}
