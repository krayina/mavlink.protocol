namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a strategy for generating C# code snippets that read data from a specific payload type (e.g., byte[], ReadOnlySpan<byte>).
/// This implements the Strategy design pattern.
/// </summary>
public interface IDeserializationPayloadReadScribanStrategy
{
	/// <summary>
	/// Gets a C# expression to read a scalar value of a given type from a specific offset.
	/// </summary>
	/// <param name="typeName">The C# type name of the value to read (e.g., "int", "ushort").</param>
	/// <param name="offset">The byte offset from the beginning of the payload.</param>
	/// <param name="size">The size in bytes of the type.</param>
	/// <returns>A string containing the C# code to read the value.</returns>
	string GenerateScalarReadExpression(string typeName, int offset, int size);

	/// <summary>
	/// Gets a C# expression to read an element from an array at a specific index.
	/// </summary>
	/// <param name="elementTypeName">The C# type name of the element (e.g., "int", "byte").</param>
	/// <param name="baseOffset">The starting byte offset of the entire array.</param>
	/// <param name="elementSize">The size in bytes of a single element.</param>
	/// <param name="indexVariablePlaceholder">A placeholder string that will be replaced with the actual loop index variable name.</param>
	/// <returns>A string containing the C# code to read the array element.</returns>
	string GenerateArrayElementReadExpression(string elementTypeName, int baseOffset, int elementSize, string indexVariablePlaceholder);

	/// <summary>
	/// Gets a complete C# code block for reading a null-terminated string from a char array field.
	/// </summary>
	/// <param name="variableName">The desired name for the final variable holding the ImmutableArray&lt;char&gt;.</param>
	/// <param name="offset">The starting byte offset of the char array in the payload.</param>
	/// <param name="maxLength">The maximum length of the array in bytes.</param>
	/// <returns>A complete, multi-line C# code block that performs the read operation.</returns>
	string GenerateTerminatedStringReadBlock(string variableName, int offset, int maxLength);
}
