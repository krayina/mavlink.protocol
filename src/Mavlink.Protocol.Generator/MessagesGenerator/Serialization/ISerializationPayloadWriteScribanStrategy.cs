namespace Mavlink.Protocol.Generator;

/// <summary>
/// Defines a strategy for generating C# code snippets that write data into a specific payload type (e.g., byte[], Span<byte>).
/// This interface is the serialization counterpart to <c>IDeserializationPayloadReadScribanStrategy</c>.
/// </summary>
public interface ISerializationPayloadWriteScribanStrategy
{
	/// <summary>
	/// Generates a C# statement to write a scalar value into the payload at a specific offset.
	/// </summary>
	/// <param name="sourceExpression">The C# expression for the value to be written (e.g., "message.Altitude", "tempValue").</param>
	/// <param name="typeName">The C# type name of the value (e.g., "int", "ushort").</param>
	/// <param name="offset">The byte offset from the beginning of the payload.</param>
	/// <returns>A string containing the C# statement to write the value.</returns>
	string GenerateScalarWriteStatement(string sourceExpression, string typeName, int offset);

	/// <summary>
	/// Generates a C# statement to write an array element into the payload, typically used inside a loop.
	/// </summary>
	/// <param name="sourceExpression">The C# expression for the element value (e.g., "message.MyArray[i]").</param>
	/// <param name="elementTypeName">The C# type name of the element.</param>
	/// <param name="baseOffset">The starting byte offset of the entire array.</param>
	/// <param name="elementSize">The size in bytes of a single element.</param>
	/// <param name="indexVariable">The name of the loop index variable (e.g., "i").</param>
	/// <returns>A string containing the C# statement to write the array element.</returns>
	string GenerateArrayElementWriteStatement(string sourceExpression, string elementTypeName, int baseOffset, int elementSize, string indexVariable);

	/// <summary>
	/// Generates a complete C# code block for writing a null-terminated string from a char array field.
	/// </summary>
	/// <param name="sourcePropertyExpression">The C# expression for the source property, which is expected to be an ImmutableArray&lt;char&gt; (e.g., "message.MyStringField").</param>
	/// <param name="offset">The starting byte offset in the payload.</param>
	/// <param name="maxLength">The maximum length of the array in bytes (the allocated space in the payload).</param>
	/// <returns>A complete, multi-line C# code block that performs the write operation, handling truncation.</returns>
	string GenerateTerminatedStringWriteBlock(string sourcePropertyExpression, int offset, int maxLength);
}
