using FluentAssertions;

namespace Mavlink.Protocol.Generator.Tests.Unit.Deserializtion;

public class BufferDeserializationPayloadReadScribanStrategyTests
{
	private readonly BufferDeserializationPayloadReadScribanStrategy _strategy;

	public BufferDeserializationPayloadReadScribanStrategyTests()
	{
		_strategy = new BufferDeserializationPayloadReadScribanStrategy();
	}

	[Theory]
	[InlineData("bool", 0, 2, "i")]
	[InlineData("decimal", 4, 16, "idx")]
	public void GenerateArrayElementReadExpression_UnsupportedType_ThrowsNotSupportedException(
	string elementTypeName, int baseOffset, int elementSize, string indexVariable)
	{
		// Act
		Action act = () => _strategy.GenerateArrayElementReadExpression(
			elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		act.Should().Throw<NotSupportedException>()
			.WithMessage($"Unsupported type: {elementTypeName}");
	}

	[Theory]
	[InlineData("byte", 0, 1, "payload[0]")]
	[InlineData("byte", 10, 1, "payload[10]")]
	[InlineData("sbyte", 0, 1, "(sbyte)payload[0]")]
	[InlineData("sbyte", 5, 1, "(sbyte)payload[5]")]
	public void GenerateScalarReadExpression_SingleByteTypes_ReturnsCorrectExpression(
		string typeName, int offset, int size, string expected)
	{
		// Act
		var result = _strategy.GenerateScalarReadExpression(typeName, offset, size);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("int", 0, 4, "System.BitConverter.ToInt32(payload, 0)")]
	[InlineData("uint", 4, 4, "System.BitConverter.ToUInt32(payload, 4)")]
	[InlineData("short", 8, 2, "System.BitConverter.ToInt16(payload, 8)")]
	[InlineData("ushort", 10, 2, "System.BitConverter.ToUInt16(payload, 10)")]
	[InlineData("long", 12, 8, "System.BitConverter.ToInt64(payload, 12)")]
	[InlineData("ulong", 20, 8, "System.BitConverter.ToUInt64(payload, 20)")]
	[InlineData("float", 28, 4, "System.BitConverter.ToSingle(payload, 28)")]
	[InlineData("double", 32, 8, "System.BitConverter.ToDouble(payload, 32)")]
	[InlineData("char", 40, 2, "System.BitConverter.ToChar(payload, 40)")]
	public void GenerateScalarReadExpression_MultiByteTypes_ReturnsBitConverterCall(
		string typeName, int offset, int size, string expected)
	{
		// Act
		var result = _strategy.GenerateScalarReadExpression(typeName, offset, size);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("decimal", 16)]
	[InlineData("bool", 1)]
	[InlineData("string", 0)]
	public void GenerateScalarReadExpression_UnsupportedTypes_ThrowsNotSupportedException(
		string typeName, int size)
	{
		// Act
		Action act = () => _strategy.GenerateScalarReadExpression(typeName, 0, size);

		// Assert
		act.Should().Throw<NotSupportedException>()
			.WithMessage($"Unsupported type: {typeName}");
	}

	[Theory]
	[InlineData("byte", 0, 1, "i", "payload[0 + i]")]
	[InlineData("byte", 10, 1, "idx", "payload[10 + idx]")]
	[InlineData("sbyte", 5, 1, "j", "(sbyte)payload[5 + j]")]
	public void GenerateArrayElementReadExpression_SingleByteElements_ReturnsIndexedAccess(
		string elementTypeName, int baseOffset, int elementSize, string indexVariable, string expected)
	{
		// Act
		var result = _strategy.GenerateArrayElementReadExpression(
			elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("int", 0, 4, "i", "System.BitConverter.ToInt32(payload, 0 + i * 4)")]
	[InlineData("ushort", 20, 2, "idx", "System.BitConverter.ToUInt16(payload, 20 + idx * 2)")]
	[InlineData("float", 8, 4, "j", "System.BitConverter.ToSingle(payload, 8 + j * 4)")]
	public void GenerateArrayElementReadExpression_MultiByteElements_CalculatesOffset(
		string elementTypeName, int baseOffset, int elementSize, string indexVariable, string expected)
	{
		// Act
		var result = _strategy.GenerateArrayElementReadExpression(
			elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void GenerateTerminatedStringReadBlock_CreatesCompleteCodeBlock()
	{
		// Arrange
		var variableName = "testString";
		var offset = 10;
		var maxLength = 32;

		// Act
		var result = _strategy.GenerateTerminatedStringReadBlock(variableName, offset, maxLength);

		// Assert
		result.Should().Contain($"var {variableName}Terminator = System.Array.IndexOf(payload, (byte)0, {offset}, {maxLength});");
		result.Should().Contain($"var {variableName}Length = {variableName}Terminator == -1 ? {maxLength} : {variableName}Terminator - {offset};");
		result.Should().Contain($"var {variableName}String = System.Text.Encoding.ASCII.GetString(payload, {offset}, {variableName}Length);");
		result.Should().Contain($"var {variableName} = System.Collections.Immutable.ImmutableArray.CreateRange({variableName}String);");
	}

	[Fact]
	public void GenerateTerminatedStringReadBlock_HandlesEmptyStringCase()
	{
		// Arrange
		var variableName = "emptyStr";
		var offset = 0;
		var maxLength = 10;

		// Act
		var result = _strategy.GenerateTerminatedStringReadBlock(variableName, offset, maxLength);

		// Assert
		result.Should().Contain("emptyStrTerminator == -1");
		result.Should().Contain("System.Text.Encoding.ASCII.GetString");
	}

	[Fact]
	public void GenerateTerminatedStringReadBlock_UsesCorrectVariableNaming()
	{
		// Arrange
		var variableName = "myField";
		var offset = 5;
		var maxLength = 20;

		// Act
		var result = _strategy.GenerateTerminatedStringReadBlock(variableName, offset, maxLength);

		// Assert
		result.Should().Contain("myFieldTerminator");
		result.Should().Contain("myFieldLength");
		result.Should().Contain("myFieldString");
		result.Should().Contain("var myField =");
	}

	[Theory]
	[InlineData("int", 2)]
	[InlineData("float", 3)]
	[InlineData("char", 3)]
	public void GenerateScalarReadExpression_ForUnsupportedMultiByteType_ThrowsNotSupportedException(
		string typeName, int invalidSize)
	{
		// Act
		Action act = () => _strategy.GenerateScalarReadExpression(typeName, 0, invalidSize);

		// Assert
		act.Should().Throw<NotSupportedException>();
	}
}
