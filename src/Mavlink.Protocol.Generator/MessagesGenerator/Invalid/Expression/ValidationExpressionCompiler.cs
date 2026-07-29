namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete implementation of the expression compiler. It uses a factory pattern
/// to delegate the compilation of a specific rule definition to a dedicated operation class.
/// </summary>
public class MavlinkMessageFieldValidationExpressionCompiler : IMavlinkMessageFieldValidationExpressionCompiler
{
	private readonly IInvalidValueExpressionBuilder _expressionBuilder;

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationExpressionCompiler"/> class.
	/// </summary>
	/// <param name="expressionBuilder">A service that handles the translation of raw MAVLink
	/// invalid values (like "UINT16_MAX") into C# literals and conditions.</param>
	public MavlinkMessageFieldValidationExpressionCompiler(IInvalidValueExpressionBuilder expressionBuilder)
	{
		_expressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
	}

	/// <inheritdoc/>
	public GeneratedMavlinkMessageFieldValidationExpression Compile(
		GeneratedMavlinkMessageFieldValidationRuleDefinition definition,
		GeneratedMavlinkMessageFieldType fieldType)
	{
		var context = new MavlinkValidationCompilationContext(fieldType, _expressionBuilder);
		return MavlinkMessageFieldValidationCompilationFactory.CreateOperation(definition).Compile(context);
	}
}
