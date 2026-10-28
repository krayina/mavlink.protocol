namespace Mavlink.Protocol.Generator;

/// <summary>
/// An implementation of <see cref="ISerializationPayloadWriteScribanStrategy"/> for writing to a standard byte array (byte[]).
/// This strategy primarily uses <see cref="System.BitConverter"/>.
/// </summary>
public class BufferSerializationPayloadWriteScribanStrategy : ISerializationPayloadWriteScribanStrategy
{
	private const string PayloadParameterName = "buffer";

	/// <inheritdoc/>
	/// <example>
	/// <code>
	/// GenerateScalarWriteStatement("message.Altitude", "float", 12)
	/// // returns: "System.BitConverter.GetBytes(message.Altitude).CopyTo(buffer, 12);"
	/// </code>
	/// </example>
	public string GenerateScalarWriteStatement(string sourceExpression, string typeName, int offset)
	{
		if (typeName is "byte" or "sbyte")
		{
			return $"{PayloadParameterName}[{offset}] = (byte)({sourceExpression});";
		}

		return $"System.BitConverter.GetBytes({sourceExpression}).CopyTo({PayloadParameterName}, {offset});";
	}

	/// <inheritdoc/>
	public string GenerateArrayElementWriteStatement(string sourceExpression, string elementTypeName, int baseOffset, int elementSize, string indexVariable)
	{
		string currentOffsetExpression = $"{baseOffset} + {indexVariable} * {elementSize}";

		if (elementSize == 1)
		{
			return $"{PayloadParameterName}[{currentOffsetExpression}] = (byte)({sourceExpression});";
		}

		return $"System.BitConverter.GetBytes({sourceExpression}).CopyTo({PayloadParameterName}, {currentOffsetExpression});";
	}

	/// <inheritdoc/>
	public string GenerateTerminatedStringWriteBlock(string sourcePropertyExpression, int offset, int maxLength)
	{
		return $@"
var chars = {sourcePropertyExpression}.Take({maxLength}).ToArray();
System.Text.Encoding.ASCII.GetBytes(chars, 0, chars.Length, {PayloadParameterName}, {offset});";
	}
}
