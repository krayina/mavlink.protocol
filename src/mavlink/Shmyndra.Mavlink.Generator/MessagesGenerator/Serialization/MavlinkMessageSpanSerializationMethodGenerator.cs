using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates Mavlink message serialization methods using the Span-based approach (with BinaryPrimitives).
/// </summary>
public class MavlinkMessageSpanSerializationMethodGenerator : MavlinkMessageSerializationMethodGeneratorBase
{
	/// <summary>
	/// Appends the prologue for serialization, initializing the buffer and creating a Span.
	/// </summary>
	protected override void AppendMethodPrologue(StringBuilder sb, string messageName, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
		sb.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");
	}

	/// <summary>
	/// Appends serialization logic for simple fields (e.g., byte, int, float).
	/// </summary>
	protected override void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName, bool isRequired)
	{
		string typeName = simpleType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (isRequired)
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = {varName};");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($"finalSpan[{offset}] = (byte){varName};");
			}
			else if (typeName == "char")
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){varName});");
			}
			else if (typeName == "float")
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({varName}));");
			}
			else if (typeName == "double")
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({varName}));");
			}
			else
			{
				string writeMethod = GetBinaryPrimitivesWriteMethod(typeName);
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {size}), {varName});");
			}
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue)
{{
    {(typeName == "byte" ? $"finalSpan[{offset}] = {varName}.Value;" :
	  typeName == "sbyte" ? $"finalSpan[{offset}] = (byte){varName}.Value;" :
	  typeName == "char" ? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){varName}.Value);" :
	  typeName == "float" ? $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({varName}.Value));" :
	  typeName == "double" ? $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({varName}.Value));" :
	  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {varName}.Value);")}
}}
else
{{
    finalSpan.Slice({offset}, {size}).Clear();
}}");
		}

		offset += size;
	}

	/// <summary>
	/// Appends serialization logic for enum fields.
	/// </summary>
	protected override void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string varName, string currentNamespace, bool isRequired)
	{
		string convertedType = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(convertedType);

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = size * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);

			sb.AppendLine($"{combinedType} combined_{varName} = 0;");
			sb.AppendLine($"for (int i = 0; i < {varName}.Length; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    combined_{varName} |= (({combinedType}){varName}[i]) << (i * {size * 8});");
			sb.AppendLine("}");
			string writeMethod = GetBinaryPrimitivesWriteMethod(combinedType);
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {size}), combined_{varName});");
		}
		else if (isRequired)
		{
			if (convertedType == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = ({convertedType}){varName};");
			}
			else if (convertedType == "sbyte")
			{
				sb.AppendLine($"finalSpan[{offset}] = (byte)({convertedType}){varName};");
			}
			else
			{
				string writeMethod = GetBinaryPrimitivesWriteMethod(convertedType);
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {size}), ({convertedType}){varName});");
			}
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue)
{{
    {(convertedType == "byte" ? $"finalSpan[{offset}] = ({convertedType}){varName}.Value;" :
	  convertedType == "sbyte" ? $"finalSpan[{offset}] = (byte)({convertedType}){varName}.Value;" :
	  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(convertedType)}(finalSpan.Slice({offset}, {size}), ({convertedType}){varName}.Value);")}
}}
else
{{
    finalSpan.Slice({offset}, {size}).Clear();
}}");
		}

		offset += size;
	}

	/// <summary>
	/// Appends serialization logic for array fields.
	/// </summary>
	protected override void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName, bool isRequired)
	{
		string elementType = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementType);
		int totalSize = arrayType.ArrayLength * elementSize;

		if (isRequired)
		{
			sb.AppendLine(GenerateArraySerialization(varName, elementType, arrayType.ArrayLength, offset, true));
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue && !{varName}.Value.IsDefaultOrEmpty)
{{
    {GenerateArraySerialization($"{varName}.Value", elementType, arrayType.ArrayLength, offset, false)}
}}
else
{{
    finalSpan.Slice({offset}, {totalSize}).Clear();
}}");
		}

		offset += totalSize;
	}

	/// <summary>
	/// Appends serialization logic for array of enum fields.
	/// </summary>
	protected override void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string varName, bool isRequired)
	{
		string elementType = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementType);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = arrayEnumType.ArrayLength * elementSize * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);

			sb.AppendLine($"{combinedType} combined_{varName} = 0;");
			sb.AppendLine($"for (int i = 0; i < {varName}.Length; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    combined_{varName} |= (({combinedType}){varName}[i]) << (i * {elementSize * 8});");
			sb.AppendLine("}");
			string writeMethod = GetBinaryPrimitivesWriteMethod(combinedType);
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {totalSize}), combined_{varName});");
		}
		else if (isRequired)
		{
			sb.AppendLine(GenerateArraySerialization(varName, elementType, arrayEnumType.ArrayLength, offset, true));
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue && !{varName}.Value.IsDefaultOrEmpty)
{{
    {GenerateArraySerialization($"{varName}.Value", elementType, arrayEnumType.ArrayLength, offset, false)}
}}
else
{{
    finalSpan.Slice({offset}, {totalSize}).Clear();
}}");
		}

		offset += totalSize;
	}

	/// <summary>
	/// Generates serialization code for arrays.
	/// </summary>
	private static string GenerateArraySerialization(string arrayName, string elementType, int arrayLength, int offset, bool isRequired)
	{
		int typeSize = Utilities.GetDotNetTypeSize(elementType);

		if (elementType == "byte")
		{
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    finalSpan[{offset} + i] = {arrayName}[i];
}}";
		}
		else if (elementType == "sbyte")
		{
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    finalSpan[{offset} + i] = (byte){arrayName}[i];
}}";
		}
		else if (elementType == "float")
		{
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), BitConverter.SingleToInt32Bits({arrayName}[i]));
}}";
		}
		else if (elementType == "double")
		{
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), BitConverter.DoubleToInt64Bits({arrayName}[i]));
}}";
		}
		else if (elementType == "char")
		{
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), (ushort){arrayName}[i]);
}}";
		}
		else
		{
			string writeMethod = GetBinaryPrimitivesWriteMethod(elementType);
			return $@"
for (int i = 0; i < {arrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), {arrayName}[i]);
}}";
		}
	}

	/// <summary>
	/// Returns the appropriate BinaryPrimitives write method for a given type.
	/// </summary>
	private static string GetBinaryPrimitivesWriteMethod(string typeName)
	{
		return typeName switch
		{
			"short" => "WriteInt16LittleEndian",
			"ushort" => "WriteUInt16LittleEndian",
			"int" => "WriteInt32LittleEndian",
			"uint" => "WriteUInt32LittleEndian",
			"long" => "WriteInt64LittleEndian",
			"ulong" => "WriteUInt64LittleEndian",
			_ => throw new NotSupportedException($"Type '{typeName}' is not supported for BinaryPrimitives serialization.")
		};
	}

	/// <summary>
	/// Returns the appropriate combined type based on the total number of bits required.
	/// </summary>
	private static string GetCombinedTypeForTotalBits(int totalBits)
	{
		if (totalBits <= 8)
		{
			return "byte";
		}
		else if (totalBits <= 16)
		{
			return "ushort";
		}
		else if (totalBits <= 32)
		{
			return "uint";
		}
		else
		{
			return "ulong";
		}
	}

	/// <summary>
	/// Appends the return statement for the serialized buffer.
	/// </summary>
	protected override void AppendReturnStatement(StringBuilder sb)
	{
		sb.AppendLine("return buffer;");
	}
}
