using FluentAssertions;

namespace Shmyndra.Mavlink.Generator.Tests.Unit.Deserializtion;

public class SpanDeserializationPayloadReadScribanStrategyTests
{
	private readonly SpanDeserializationPayloadReadScribanStrategy _strategy;

	public SpanDeserializationPayloadReadScribanStrategyTests()
	{
		_strategy = new SpanDeserializationPayloadReadScribanStrategy();
	}

	[Theory]
	[InlineData("byte", 0, 1, "span[0]")]
	[InlineData("byte", 10, 1, "span[10]")]
	[InlineData("sbyte", 0, 1, "(sbyte)span[0]")]
	[InlineData("sbyte", 5, 1, "(sbyte)span[5]")]
	public void GenerateScalarReadExpression_SingleByteTypes_ReturnsDirectIndexAccess(
		string typeName, int offset, int size, string expected)
	{
		// Act
		var result = _strategy.GenerateScalarReadExpression(typeName, offset, size);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("int", 0, 4, "System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0, 4))")]
	[InlineData("uint", 4, 4, "System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4))")]
	[InlineData("short", 8, 2, "System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(span.Slice(8, 2))")]
	[InlineData("ushort", 10, 2, "System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(10, 2))")]
	[InlineData("long", 12, 8, "System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(span.Slice(12, 8))")]
	[InlineData("ulong", 20, 8, "System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(20, 8))")]
	[InlineData("float", 28, 4, "System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span.Slice(28, 4))")]
	[InlineData("double", 32, 8, "System.Buffers.Binary.BinaryPrimitives.ReadDoubleLittleEndian(span.Slice(32, 8))")]
	[InlineData("char", 40, 2, "System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(40, 2))")]
	public void GenerateScalarReadExpression_MultiByteTypes_UsesBinaryPrimitivesLittleEndian(
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
	[InlineData("byte", 0, 1, "i", "span[0 + i]")]
	[InlineData("byte", 10, 1, "idx", "span[10 + idx]")]
	[InlineData("sbyte", 5, 1, "j", "(sbyte)span[5 + j]")]
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
	[InlineData("int", 0, 4, "i", "System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0 + i * 4, 4))")]
	[InlineData("ushort", 20, 2, "idx", "System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(20 + idx * 2, 2))")]
	[InlineData("float", 8, 4, "j", "System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(span.Slice(8 + j * 4, 4))")]
	public void GenerateArrayElementReadExpression_MultiByteElements_CalculatesOffset_AndSlicesCorrectly(
		string elementTypeName, int baseOffset, int elementSize, string indexVariable, string expected)
	{
		// Act
		var result = _strategy.GenerateArrayElementReadExpression(
			elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("bool", 0, 2)]
	[InlineData("decimal", 4, 16)]
	public void GenerateArrayElementReadExpression_UnsupportedType_ThrowsNotSupportedException(
		string elementTypeName, int baseOffset, int elementSize)
	{
		// Act
		Action act = () => _strategy.GenerateArrayElementReadExpression(
			elementTypeName, baseOffset, elementSize, "i");

		// Assert
		act.Should().Throw<NotSupportedException>()
			.WithMessage($"Unsupported type: {elementTypeName}");
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
		result.Should().Contain($"var {variableName}Slice = span.Slice({offset}, {maxLength});");
		result.Should().Contain($"var {variableName}Terminator = {variableName}Slice.IndexOf((byte)0);");
		result.Should().Contain($"var {variableName}Data = {variableName}Terminator == -1 ? {variableName}Slice : {variableName}Slice.Slice(0, {variableName}Terminator);");
		result.Should().Contain($"var {variableName}String = System.Text.Encoding.ASCII.GetString({variableName}Data);");
		result.Should().Contain($"var {variableName} = System.Collections.Immutable.ImmutableArray.CreateRange({variableName}String);");
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
		result.Should().Contain("myFieldSlice");
		result.Should().Contain("myFieldTerminator");
		result.Should().Contain("myFieldData");
		result.Should().Contain("myFieldString");
		result.Should().Contain("var myField =");
	}

	[Theory]
	[InlineData("ushort", 1)]
	[InlineData("int", 2)]
	[InlineData("double", 4)]
	[InlineData("byte", 2)]
	public void GenerateScalarReadExpression_WithInconsistentSize_ThrowsArgumentException(string typeName, int incorrectSize)
	{
		// Arrange
		var offset = 0;

		var strategy = new SpanDeserializationPayloadReadScribanStrategy();

		// Act
		Action act = () => strategy.GenerateScalarReadExpression(typeName, offset, incorrectSize);

		// Assert
		act.Should().Throw<NotSupportedException>();
	}
}
