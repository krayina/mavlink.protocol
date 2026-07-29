namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An implementation of <see cref="IDeserializationPayloadReadScribanStrategy"/> for deserializing from a standard byte array (byte[]).
/// </summary>
public class BufferDeserializationPayloadReadScribanStrategy : IDeserializationPayloadReadScribanStrategy
{
	private const string PayloadParameterName = "payload";

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
			: $"System.BitConverter.{GetBitConverterMethod(typeName)}({PayloadParameterName}, {offset})";
	}

	/// <inheritdoc/>
	public string GenerateArrayElementReadExpression(string elementTypeName, int baseOffset, int elementSize, string indexVariable)
	{
		string currentOffsetExpression = $"{baseOffset} + {indexVariable} * {elementSize}";
		return elementSize == 1
			? GenerateSingleByteReadExpression(elementTypeName, $"{baseOffset} + {indexVariable}")
			: $"System.BitConverter.{GetBitConverterMethod(elementTypeName)}({PayloadParameterName}, {currentOffsetExpression})";
	}

	/// <inheritdoc/>
	public string GenerateTerminatedStringReadBlock(string variableName, int offset, int maxLength)
	{
		string terminatorVar = $"{variableName}Terminator";
		string lengthVar = $"{variableName}Length";
		string stringVar = $"{variableName}String";

		return $@"
var {terminatorVar} = System.Array.IndexOf({PayloadParameterName}, (byte)0, {offset}, {maxLength});
var {lengthVar} = {terminatorVar} == -1 ? {maxLength} : {terminatorVar} - {offset};
var {stringVar} = System.Text.Encoding.ASCII.GetString({PayloadParameterName}, {offset}, {lengthVar});
var {variableName} = System.Collections.Immutable.ImmutableArray.CreateRange({stringVar});";
	}

	private string GenerateSingleByteReadExpression(string typeName, string offsetExpression)
		=> typeName switch
		{
			"byte" => $"{PayloadParameterName}[{offsetExpression}]",
			"sbyte" => $"(sbyte){PayloadParameterName}[{offsetExpression}]",
			_ => throw new NotSupportedException("This helper is for single-byte types only.")
		};

	private static string GetBitConverterMethod(string typeName) => typeName switch
	{
		"int" => "ToInt32",
		"uint" => "ToUInt32",
		"short" => "ToInt16",
		"ushort" => "ToUInt16",
		"long" => "ToInt64",
		"ulong" => "ToUInt64",
		"float" => "ToSingle",
		"double" => "ToDouble",
		"char" => "ToChar",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
