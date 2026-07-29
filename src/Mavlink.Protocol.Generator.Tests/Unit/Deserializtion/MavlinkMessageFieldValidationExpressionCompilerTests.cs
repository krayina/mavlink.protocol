using FluentAssertions;

namespace Mavlink.Protocol.Generator.Tests.Unit.Deserializtion;

public class MavlinkMessageFieldValidationExpressionCompilerTests
{
	[Fact]
	public void Compile_NoValidationRule_ReturnsNoValidationExpression()
	{
		// Arrange
		var builder = new InvalidValueExpressionBuilder();
		var compiler = new MavlinkMessageFieldValidationExpressionCompiler(builder);
		var rule = new GeneratedMavlinkMessageNoValidationRuleDefinition();
		var fieldType = new GeneratedMavlinkMessageFieldPrimitiveType("int", Original: null!);

		// Act
		var expr = compiler.Compile(rule, fieldType);

		// Assert
		expr.Should().BeOfType<GeneratedMavlinkMessageFieldNoValidationExpression>();
	}

	[Theory]
	[InlineData("int", "-1", "value != -1")]
	[InlineData("float", "NaN", "!float.IsNaN(value)")]
	public void Compile_WholeFieldRule_ReturnsWholeValidationExpression_WithCondition(
		string convertedType, string rawInvalid, string expectedCondition)
	{
		// Arrange
		var builder = new InvalidValueExpressionBuilder();
		var compiler = new MavlinkMessageFieldValidationExpressionCompiler(builder);
		var rule = new GeneratedMavlinkMessageWholeFieldValidationRuleDefinition(rawInvalid);
		var fieldType = new GeneratedMavlinkMessageFieldPrimitiveType(convertedType, Original: null!);

		// Act
		var expr = compiler.Compile(rule, fieldType);

		// Assert
		expr.Should().BeOfType<GeneratedMavlinkMessageFieldWholeValidationExpression>();
		var whole = (GeneratedMavlinkMessageFieldWholeValidationExpression)expr;
		whole.ConditionForWholeField.Should().Be(expectedCondition);
	}

	[Fact]
	public void Compile_PerElementRule_ReturnsPerElementValidationExpression_WithElementPlaceholder()
	{
		// Arrange
		var builder = new InvalidValueExpressionBuilder();
		var compiler = new MavlinkMessageFieldValidationExpressionCompiler(builder);
		var rule = new GeneratedMavlinkMessagePerElementValidationRuleDefinition("NaN");
		var arrayType = new GeneratedMavlinkMessageFieldArrayType(
			new GeneratedMavlinkMessageFieldPrimitiveType("float", Original: null!),
			ArrayLength: 5,
			Original: null!);

		// Act
		var expr = compiler.Compile(rule, arrayType);

		// Assert
		expr.Should().BeOfType<GeneratedMavlinkMessageFieldPerElementValidationExpression>();
		var perElem = (GeneratedMavlinkMessageFieldPerElementValidationExpression)expr;
		perElem.ElementConditionTemplate.Should().Be("!float.IsNaN({element})");
	}

	[Fact]
	public void Compile_PerIndexRule_ReturnsPerIndexValidationExpression_WithIndexConditionMap()
	{
		// Arrange
		var builder = new InvalidValueExpressionBuilder();
		var compiler = new MavlinkMessageFieldValidationExpressionCompiler(builder);
		var rule = new GeneratedMavlinkMessagePerIndexValidationRuleDefinition([null, "65535", "", "0", "NaN"]);
		var arrayType = new GeneratedMavlinkMessageFieldArrayType(
			new GeneratedMavlinkMessageFieldPrimitiveType("float", Original: null!),
			ArrayLength: 5,
			Original: null!);

		// Act
		var expr = compiler.Compile(rule, arrayType);

		// Assert
		expr.Should().BeOfType<GeneratedMavlinkMessageFieldPerIndexValidationExpression>();
		var perIndex = (GeneratedMavlinkMessageFieldPerIndexValidationExpression)expr;

		perIndex.ConditionByIndex.Keys.Should().BeEquivalentTo([1, 3, 4]);
		perIndex.ConditionByIndex[1].Should().Be("element != 65535f");
		perIndex.ConditionByIndex[3].Should().Be("element != 0f");
		perIndex.ConditionByIndex[4].Should().Be("!float.IsNaN(element)");
	}

	[Fact]
	public void Compile_DelegatesToExpressionBuilder_WithCorrectVariableNames_AndTypes()
	{
		// Arrange
		var spy = new SpyInvalidValueExpressionBuilder();
		var compiler = new MavlinkMessageFieldValidationExpressionCompiler(spy);

		var wholeRule = new GeneratedMavlinkMessageWholeFieldValidationRuleDefinition("-1");
		var perElemRule = new GeneratedMavlinkMessagePerElementValidationRuleDefinition("NaN");
		var perIndexRule = new GeneratedMavlinkMessagePerIndexValidationRuleDefinition(["0", null, "65535"]);

		var intType = new GeneratedMavlinkMessageFieldPrimitiveType("int", Original: null!);
		var floatArrayType = new GeneratedMavlinkMessageFieldArrayType(
			new GeneratedMavlinkMessageFieldPrimitiveType("float", Original: null!),
			ArrayLength: 3,
			Original: null!);

		// Act
		compiler.Compile(wholeRule, intType);
		compiler.Compile(perElemRule, floatArrayType);
		compiler.Compile(perIndexRule, floatArrayType);

		// Assert
		spy.Calls.Should().ContainEquivalentOf(new SpyInvalidValueExpressionBuilder.Call(
			VariableName: "value", RawInvalidValue: "-1", ConvertedType: "int"));

		spy.Calls.Should().ContainEquivalentOf(new SpyInvalidValueExpressionBuilder.Call(
			VariableName: "{element}", RawInvalidValue: "NaN", ConvertedType: "float"));

		// For per-index: two calls at indices 0 and 2 (null at index 1 is skipped)
		spy.Calls.Should().ContainEquivalentOf(new SpyInvalidValueExpressionBuilder.Call(
			VariableName: "element", RawInvalidValue: "0", ConvertedType: "float"));
		spy.Calls.Should().ContainEquivalentOf(new SpyInvalidValueExpressionBuilder.Call(
			VariableName: "element", RawInvalidValue: "65535", ConvertedType: "float"));
	}

	private sealed class SpyInvalidValueExpressionBuilder : IInvalidValueExpressionBuilder
	{
		public readonly List<Call> Calls = new();

		public string BuildCondition(string variableName, string rawInvalidValue, GeneratedMavlinkMessageFieldType type)
		{
			Calls.Add(new Call(variableName, rawInvalidValue, type.ConvertedType));
			return $"{variableName} != {rawInvalidValue}";
		}

		public readonly record struct Call(string VariableName, string RawInvalidValue, string ConvertedType);
	}
}
