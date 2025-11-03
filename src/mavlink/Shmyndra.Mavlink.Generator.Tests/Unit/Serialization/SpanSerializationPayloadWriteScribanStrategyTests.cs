using FluentAssertions;

namespace Shmyndra.Mavlink.Generator.Tests.Unit.Serialization;

public class SpanSerializationPayloadWriteScribanStrategyTests
{
	private readonly SpanSerializationPayloadWriteScribanStrategy _sut = new();

	[Theory]
	[InlineData("int", "WriteInt32LittleEndian")]
	[InlineData("uint", "WriteUInt32LittleEndian")]
	[InlineData("short", "WriteInt16LittleEndian")]
	[InlineData("ushort", "WriteUInt16LittleEndian")]
	[InlineData("long", "WriteInt64LittleEndian")]
	[InlineData("ulong", "WriteUInt64LittleEndian")]
	[InlineData("float", "WriteSingleLittleEndian")]
	[InlineData("double", "WriteDoubleLittleEndian")]
	[InlineData("char", "WriteUInt16LittleEndian")]
	public void GenerateScalarWriteStatement_ShouldReturnCorrectBinaryPrimitivesCall_ForMultiByteTypes(string typeName, string writeMethodName)
	{
		// Arrange
		const string sourceExpression = "message.MyField";
		const int offset = 10;
		int size = Utilities.GetDotNetTypeSize(typeName); // Assuming this helper exists

		// Act
		string result = _sut.GenerateScalarWriteStatement(sourceExpression, typeName, offset);

		// Assert
		result.Should().Be($"System.Buffers.Binary.BinaryPrimitives.{writeMethodName}(span.Slice({offset}, {size}), {sourceExpression});");
	}

	[Theory]
	[InlineData("byte")]
	[InlineData("sbyte")]
	public void GenerateScalarWriteStatement_ShouldReturnDirectAssignment_ForSingleByteTypes(string typeName)
	{
		// Arrange
		const string sourceExpression = "value";
		const int offset = 5;

		// Act
		string result = _sut.GenerateScalarWriteStatement(sourceExpression, typeName, offset);

		// Assert
		result.Should().Be($"span[{offset}] = (byte)({sourceExpression});");
	}

	[Theory]
	[InlineData("int", "WriteInt32LittleEndian", 4)]
	[InlineData("ushort", "WriteUInt16LittleEndian", 2)]
	[InlineData("double", "WriteDoubleLittleEndian", 8)]
	public void GenerateArrayElementWriteStatement_ShouldReturnCorrectBinaryPrimitivesCall_ForMultiByteElements(string elementTypeName, string writeMethodName, int elementSize)
	{
		// Arrange
		const string sourceExpression = "array[i]";
		const string indexVariable = "i";
		const int baseOffset = 16;

		// Act
		string result = _sut.GenerateArrayElementWriteStatement(sourceExpression, elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		string expectedOffset = $"{baseOffset} + {indexVariable} * {elementSize}";
		result.Should().Be($"System.Buffers.Binary.BinaryPrimitives.{writeMethodName}(span.Slice({expectedOffset}, {elementSize}), {sourceExpression});");
	}

	[Fact]
	public void GenerateArrayElementWriteStatement_ShouldReturnDirectAssignment_ForSingleByteElements()
	{
		// Arrange
		const string sourceExpression = "byteArray[i]";
		const string indexVariable = "i";
		const int baseOffset = 16;
		const int elementSize = 1;

		// Act
		string result = _sut.GenerateArrayElementWriteStatement(sourceExpression, "byte", baseOffset, elementSize, indexVariable);

		// Assert
		string expectedOffset = $"{baseOffset} + {indexVariable} * {elementSize}";
		result.Should().Be($"span[{expectedOffset}] = (byte)({sourceExpression});");
	}

	[Fact]
	public void GenerateTerminatedStringWriteBlock_ShouldGenerateCorrectSpanEncodingBlock()
	{
		// Arrange
		const string sourceProperty = "message.MyString";
		const int offset = 30;
		const int maxLength = 10;

		// Act
		string result = _sut.GenerateTerminatedStringWriteBlock(sourceProperty, offset, maxLength);

		// Assert
		result.Should().Contain($"var sourceChars = {sourceProperty}.AsSpan();");
		result.Should().Contain($"if (sourceChars.Length > {maxLength}) {{ sourceChars = sourceChars.Slice(0, {maxLength}); }}");
		result.Should().Contain($"System.Text.Encoding.ASCII.GetBytes(sourceChars, span.Slice({offset}, {maxLength}));");
	}
}
