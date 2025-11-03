namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An implementation of <see cref="ISerializationPayloadWriteScribanStrategy"/> for writing to a <see cref="Span{T}"/>.
/// This strategy primarily uses <see cref="System.Buffers.Binary.BinaryPrimitives"/>.
/// </summary>
public class SpanSerializationPayloadWriteScribanStrategy : ISerializationPayloadWriteScribanStrategy
{
	private const string PayloadParameterName = "span";

	/// <inheritdoc/>
	/// <example>
	/// <code>
	/// GenerateScalarWriteStatement("message.Altitude", "float", 12)
	/// // returns: "System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(span.Slice(12, 4), message.Altitude);"
	/// </code>
	/// </example>
	public string GenerateScalarWriteStatement(string sourceExpression, string typeName, int offset)
	{
		if (typeName is "byte" or "sbyte")
		{
			return $"{PayloadParameterName}[{offset}] = (byte)({sourceExpression});";
		}

		int size = Utilities.GetDotNetTypeSize(typeName);
		string writeMethod = GetBinaryPrimitivesWriteMethod(typeName);
		return $"System.Buffers.Binary.BinaryPrimitives.{writeMethod}({PayloadParameterName}.Slice({offset}, {size}), {sourceExpression});";
	}

	/// <inheritdoc/>
	public string GenerateArrayElementWriteStatement(string sourceExpression, string elementTypeName, int baseOffset, int elementSize, string indexVariable)
	{
		string currentOffsetExpression = $"{baseOffset} + {indexVariable} * {elementSize}";

		if (elementSize == 1)
		{
			return $"{PayloadParameterName}[{currentOffsetExpression}] = (byte)({sourceExpression});";
		}

		string writeMethod = GetBinaryPrimitivesWriteMethod(elementTypeName);
		return $"System.Buffers.Binary.BinaryPrimitives.{writeMethod}({PayloadParameterName}.Slice({currentOffsetExpression}, {elementSize}), {sourceExpression});";
	}

	/// <inheritdoc/>
	public string GenerateTerminatedStringWriteBlock(string sourcePropertyExpression, int offset, int maxLength)
	{
		return $@"
var sourceChars = {sourcePropertyExpression}.AsSpan();
if (sourceChars.Length > {maxLength}) {{ sourceChars = sourceChars.Slice(0, {maxLength}); }}
System.Text.Encoding.ASCII.GetBytes(sourceChars, {PayloadParameterName}.Slice({offset}, {maxLength}));";
	}

	private static string GetBinaryPrimitivesWriteMethod(string typeName) => typeName switch
	{
		"int" => "WriteInt32LittleEndian",
		"uint" => "WriteUInt32LittleEndian",
		"short" => "WriteInt16LittleEndian",
		"ushort" => "WriteUInt16LittleEndian",
		"char" => "WriteUInt16LittleEndian",
		"long" => "WriteInt64LittleEndian",
		"ulong" => "WriteUInt64LittleEndian",
		"float" => "WriteSingleLittleEndian",
		"double" => "WriteDoubleLittleEndian",
		_ => throw new NotSupportedException($"Unsupported type for BinaryPrimitives write: {typeName}")
	};
}
