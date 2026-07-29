using FluentAssertions;

namespace Mavlink.Protocol.Generator.Tests.Unit.Deserializtion;

public class InvalidValueExpressionBuilderTests
{
	private readonly InvalidValueExpressionBuilder _builder = new();

	[Fact]
	public void BuildCondition_NaN_ForFloatType_UsesFloatIsNaN_Negated()
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("float", Original: null!);

		// Act
		var result = _builder.BuildCondition("val", "NaN", type);

		// Assert
		result.Should().Be("!float.IsNaN(val)");
	}

	[Fact]
	public void BuildCondition_NaN_ForDoubleType_UsesDoubleIsNaN_Negated()
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("double", Original: null!);

		// Act
		var result = _builder.BuildCondition("x", "NaN", type);

		// Assert
		result.Should().Be("!double.IsNaN(x)");
	}

	[Fact]
	public void BuildCondition_NaN_ForNonFloatingType_ThrowsFormatException_WithDescriptiveMessage()
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("int", Original: null!);

		// Act
		Action act = () => _builder.BuildCondition("v", "NaN", type);

		// Assert
		act.Should().Throw<FormatException>()
		   .WithMessage("The invalid value 'NaN' is only applicable to floating-point types, but was used with 'int'.");
	}

	[Theory]
	[InlineData("UINT8_MAX", "byte.MaxValue")]
	[InlineData("UINT16_MAX", "ushort.MaxValue")]
	[InlineData("UINT32_MAX", "uint.MaxValue")]
	[InlineData("UINT64_MAX", "ulong.MaxValue")]
	[InlineData("INT8_MAX", "sbyte.MaxValue")]
	[InlineData("INT16_MAX", "short.MaxValue")]
	[InlineData("INT32_MAX", "int.MaxValue")]
	[InlineData("INT64_MAX", "long.MaxValue")]
	public void BuildCondition_KnownMavlinkConstants_AreTranslatedToClrMaxValues(string rawInvalid, string expectedLiteral)
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("int", Original: null!);

		// Act
		var result = _builder.BuildCondition("value", rawInvalid, type);

		// Assert
		result.Should().Be($"value != {expectedLiteral}");
	}

	[Theory]
	[InlineData("1", "1")]
	[InlineData("-42", "-42")]
	[InlineData("1.5", "1.5")]
	[InlineData("1e-3", "0.001")]
	public void BuildCondition_NumericLiteral_InvariantCulture_IsUsed(string literal, string expectedLiteral)
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("double", Original: null!);

		// Act
		var result = _builder.BuildCondition("x", literal, type);

		// Assert
		result.Should().Be($"x != {expectedLiteral}");
	}

	[Fact]
	public void BuildCondition_NumericLiteral_CommaAsDecimalSeparator_IsRejected()
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("double", Original: null!);

		// Act
		Action act = () => _builder.BuildCondition("x", "1,5", type);

		// Assert
		act.Should().Throw<FormatException>();
	}

	[Fact]
	public void BuildCondition_EnumField_WrapsVariableWithConvertedEnumTypeCast()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldEnumType("ushort", GeneratedEnum: null!, Original: null!);

		// Act
		var result = _builder.BuildCondition("e", "65535", enumType);

		// Assert
		result.Should().Be("(ushort)e != 65535");
	}

	[Fact]
	public void BuildCondition_UnknownToken_ThrowsFormatException()
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("int", Original: null!);

		// Act
		Action act = () => _builder.BuildCondition("v", "FOO_BAR", type);

		// Assert
		act.Should().Throw<FormatException>()
		   .WithMessage("The raw invalid value*FOO_BAR*");
	}

	[Theory]
	[InlineData("nan")]
	[InlineData("NAN")]
	[InlineData("NaN")]
	public void BuildCondition_ConstantName_IsCaseInsensitive(string token)
	{
		// Arrange
		var type = new GeneratedMavlinkMessageFieldPrimitiveType("float", Original: null!);

		// Act
		var result = _builder.BuildCondition("val", token, type);

		// Assert
		result.Should().Be("!float.IsNaN(val)");
	}
}
