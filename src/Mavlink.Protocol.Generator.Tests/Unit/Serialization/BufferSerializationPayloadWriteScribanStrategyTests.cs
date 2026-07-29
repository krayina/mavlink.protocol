using FluentAssertions;

namespace Mavlink.Protocol.Generator;

public class BufferSerializationPayloadWriteScribanStrategyTests
{
	private readonly BufferSerializationPayloadWriteScribanStrategy _sut = new();

	[Theory]
	[InlineData("int", "message.MyField", 10)]
	[InlineData("uint", "tempValue", 20)]
	[InlineData("short", "message.Data", 0)]
	[InlineData("ushort", "value", 4)]
	[InlineData("long", "message.Timestamp", 8)]
	[InlineData("ulong", "id", 16)]
	[InlineData("float", "message.Altitude", 12)]
	[InlineData("double", "message.Longitude", 24)]
	[InlineData("char", "(char)value", 2)] // char is 2 bytes
	public void GenerateScalarWriteStatement_ShouldReturnCorrectCopyToStatement_ForMultiByteTypes(string typeName, string sourceExpression, int offset)
	{
		// Act
		string result = _sut.GenerateScalarWriteStatement(sourceExpression, typeName, offset);

		// Assert
		result.Should().Be($"System.BitConverter.GetBytes({sourceExpression}).CopyTo(buffer, {offset});");
	}

	[Theory]
	[InlineData("byte", "message.MyByte", 5)]
	[InlineData("sbyte", "(sbyte)tempValue", 7)]
	public void GenerateScalarWriteStatement_ShouldReturnDirectAssignment_ForSingleByteTypes(string typeName, string sourceExpression, int offset)
	{
		// Act
		string result = _sut.GenerateScalarWriteStatement(sourceExpression, typeName, offset);

		// Assert
		result.Should().Be($"buffer[{offset}] = (byte)({sourceExpression});");
	}

	[Theory]
	[InlineData("int", 4, 10)]
	[InlineData("ushort", 2, 20)]
	[InlineData("double", 8, 0)]
	public void GenerateArrayElementWriteStatement_ShouldReturnCorrectCopyToStatement_ForMultiByteElements(string elementTypeName, int elementSize, int baseOffset)
	{
		// Arrange
		const string sourceExpression = "array[i]";
		const string indexVariable = "i";

		// Act
		string result = _sut.GenerateArrayElementWriteStatement(sourceExpression, elementTypeName, baseOffset, elementSize, indexVariable);

		// Assert
		string expectedOffset = $"{baseOffset} + {indexVariable} * {elementSize}";
		result.Should().Be($"System.BitConverter.GetBytes({sourceExpression}).CopyTo(buffer, {expectedOffset});");
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
		result.Should().Be($"buffer[{expectedOffset}] = (byte)({sourceExpression});");
	}

	[Fact]
	public void GenerateTerminatedStringWriteBlock_ShouldGenerateCorrectEncodingAndCopyBlock()
	{
		// Arrange
		const string sourceProperty = "message.MyString";
		const int offset = 30;
		const int maxLength = 10;

		// Act
		string result = _sut.GenerateTerminatedStringWriteBlock(sourceProperty, offset, maxLength);

		// Assert
		result.Should().Contain($"var chars = {sourceProperty}.Take({maxLength}).ToArray();");
		result.Should().Contain($"System.Text.Encoding.ASCII.GetBytes(chars, 0, chars.Length, buffer, {offset});");
	}
}
