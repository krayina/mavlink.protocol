namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An implementation of <see cref="IDeserializationPayloadReadScribanStrategy"/> for deserializing from a ReadOnlySpan&lt;byte&gt;.
/// </summary>
public class SpanDeserializationPayloadReadScribanStrategy : IDeserializationPayloadReadScribanStrategy
{
	private const string PayloadParameterName = "span";

	/// <inheritdoc/>
	public string GenerateScalarReadExpression(string typeName, int offset, int size)
	{
		int expectedSize = Utilities.GetDotNetTypeSize(typeName);
		if (size != expectedSize)
		{
			throw new NotSupportedException($"The provided size {size} for type '{typeName}' is incorrect. Expected size is {expectedSize}.");
		}

		return size == 1
			? GenerateSingleByteReadExpression(typeName, offset.ToString())
			: $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}({PayloadParameterName}.Slice({offset}, {size}))";
	}

	/// <inheritdoc/>
	public string GenerateArrayElementReadExpression(string elementTypeName, int baseOffset, int elementSize, string indexVariablePlaceholder)
	{
		string currentOffsetExpression = $"{baseOffset} + {indexVariablePlaceholder} * {elementSize}";
		return elementSize == 1
			? GenerateSingleByteReadExpression(elementTypeName, $"{baseOffset} + {indexVariablePlaceholder}")
			: $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(elementTypeName)}({PayloadParameterName}.Slice({currentOffsetExpression}, {elementSize}))";
	}

	/// <inheritdoc/>
	public string GenerateTerminatedStringReadBlock(string variableName, int offset, int maxLength)
	{
		string sliceVar = $"{variableName}Slice";
		string terminatorVar = $"{variableName}Terminator";
		string dataVar = $"{variableName}Data";
		string stringVar = $"{variableName}String";

		return $@"
var {sliceVar} = {PayloadParameterName}.Slice({offset}, {maxLength});
var {terminatorVar} = {sliceVar}.IndexOf((byte)0);
var {dataVar} = {terminatorVar} == -1 ? {sliceVar} : {sliceVar}.Slice(0, {terminatorVar});
var {stringVar} = System.Text.Encoding.ASCII.GetString({dataVar});
var {variableName} = System.Collections.Immutable.ImmutableArray.CreateRange({stringVar});";
	}

	private string GenerateSingleByteReadExpression(string typeName, string offsetExpression) => typeName switch
	{
		"byte" => $"{PayloadParameterName}[{offsetExpression}]",
		"sbyte" => $"(sbyte){PayloadParameterName}[{offsetExpression}]",
		_ => throw new NotSupportedException("This helper is for single-byte types only.")
	};

	private static string GetBinaryPrimitivesMethod(string typeName) => typeName switch
	{
		"int" => "ReadInt32LittleEndian",
		"uint" => "ReadUInt32LittleEndian",
		"short" => "ReadInt16LittleEndian",
		"ushort" => "ReadUInt16LittleEndian",
		"char" => "ReadUInt16LittleEndian",
		"long" => "ReadInt64LittleEndian",
		"ulong" => "ReadUInt64LittleEndian",
		"float" => "ReadSingleLittleEndian",
		"double" => "ReadDoubleLittleEndian",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
