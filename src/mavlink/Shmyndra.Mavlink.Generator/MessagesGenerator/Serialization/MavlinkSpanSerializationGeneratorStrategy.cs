using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkSpanSerializationGeneratorStrategy : IMavlinkSerializationGeneratorStrategy
{
	private const int BitsPerByte = 8;

	public void AppendBufferInitialization(StringBuilder sb, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
		sb.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");
	}

	public void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace)
	{
		switch (field.Type)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType:
				AppendEnumFieldSerialization(sb, field, enumType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayFieldSerialization(sb, field, arrayType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				AppendArrayEnumFieldSerialization(sb, field, arrayEnumType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldType simpleType:
				AppendSimpleFieldSerialization(sb, field, simpleType, ref offset, variableName);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.Type.GetType().Name}' is not supported.");
		}
	}

	public void AppendReturnStatement(StringBuilder sb)
	{
		sb.AppendLine("return buffer;");
	}

	private void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string variableName)
	{
		string typeName = simpleType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.IsRequired)
		{
			AppendRequiredSimpleField(sb, variableName, typeName, offset, size);
		}
		else
		{
			AppendOptionalSimpleField(sb, variableName, typeName, offset, size);
		}

		offset += size;
	}

	private void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string variableName)
	{
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			AppendBitmaskEnumField(sb, variableName, typeName, size, offset);
		}
		else if (field.IsRequired)
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = ({typeName}){variableName};");
			}
			else
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){variableName});");
			}
		}
		else
		{
			if (typeName == "byte")
			{
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    finalSpan[{offset}] = ({typeName}){variableName}.Value;
}}");
			}
			else
			{
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){variableName}.Value);
}}");
			}
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string variableName)
	{
		string typeName = arrayType.ConvertedType;
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(typeName);

		if (field.IsRequired)
		{
			AppendRequiredArrayField(sb, variableName, typeName, arrayType.ArrayLength, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayField(sb, variableName, typeName, arrayType.ArrayLength, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string variableName)
	{
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.IsRequired)
		{
			AppendRequiredArrayEnumField(sb, variableName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayEnumField(sb, variableName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendRequiredSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($"finalSpan[{offset}] = {variableName};");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"finalSpan[{offset}] = (byte){variableName};");
		}
		else if (typeName == "char")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName});");
		}
		else if (typeName == "float")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}));");
		}
		else if (typeName == "double")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({variableName}));");
		}
		else
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {variableName});");
		}
	}

	private void AppendOptionalSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    finalSpan[{offset}] = {variableName}.Value;
}}");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    finalSpan[{offset}] = (byte){variableName}.Value;
}}");
		}
		else if (typeName == "char")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName}.Value);
}}");
		}
		else if (typeName == "float")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}.Value));
}}");
		}
		else if (typeName == "double")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({variableName}.Value));
}}");
		}
		else
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {variableName}.Value);
}}");
		}
	}

	private void AppendRequiredArrayField(StringBuilder sb, string variableName, string typeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine(GenerateArraySerialization(variableName, typeName, arrayLength, offset));
	}

	private void AppendOptionalArrayField(StringBuilder sb, string variableName, string typeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    {GenerateArraySerialization($"{variableName}.Value", typeName, arrayLength, offset)}
}}");
	}

	private void AppendRequiredArrayEnumField(StringBuilder sb, string variableName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    {elementTypeName} combinedFlags = 0;");
		sb.AppendLine($"    foreach (var flag in {variableName}[i])");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        combinedFlags |= ({elementTypeName})flag;");
		sb.AppendLine($"    }}");
		sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);");
		sb.AppendLine("}");
	}

	private void AppendOptionalArrayEnumField(StringBuilder sb, string variableName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    for (int i = 0; i < {arrayLength}; i++)
    {{
        {elementTypeName} combinedFlags = 0;
        foreach (var flag in {variableName}.Value[i])
        {{
            combinedFlags |= ({elementTypeName})flag;
        }}
        System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);
    }}
}}");
	}

	private void AppendBitmaskEnumField(StringBuilder sb, string variableName, string typeName, int size, int offset)
	{
		string combinedType = Utilities.GetCombinedTypeForTotalBits(size * BitsPerByte);
		AppendBitmask(sb, variableName, combinedType, size);
		sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(combinedType)}(finalSpan.Slice({offset}, {size}), combined{variableName});");
	}

	private void AppendBitmask(StringBuilder sb, string variableName, string combinedType, int elementSize)
	{
		sb.AppendLine($"{combinedType} combined{variableName} = 0;");
		sb.AppendLine($"for (int i = 0; i < {variableName}.Length; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    combined{variableName} |= (({combinedType}){variableName}[i]) << (i * {elementSize * BitsPerByte});");
		sb.AppendLine("}");
	}

	private string GenerateArraySerialization(string arrayName, string typeName, int arrayLength, int offset)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		if (typeName == "byte")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ finalSpan[{offset} + i] = {arrayName}[i]; }}";
		}
		else if (typeName == "sbyte")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ finalSpan[{offset} + i] = (byte){arrayName}[i]; }}";
		}
		else if (typeName == "char")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset} + i * {size}, {size}), (ushort){arrayName}[i]); }}";
		}
		else if (typeName == "float")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset} + i * {size}, {size}), BitConverter.SingleToInt32Bits({arrayName}[i]); }}";
		}
		else if (typeName == "double")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset} + i * {size}, {size}), BitConverter.DoubleToInt64Bits({arrayName}[i]); }}";
		}
		else
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset} + i * {size}, {size}), {arrayName}[i]); }}";
		}
	}

	private static string GetBinaryPrimitivesWriteMethod(string typeName)
	{
		if (typeName == "short")
		{
			return "WriteInt16LittleEndian";
		}
		else if (typeName == "ushort")
		{
			return "WriteUInt16LittleEndian";
		}
		else if (typeName == "int")
		{
			return "WriteInt32LittleEndian";
		}
		else if (typeName == "uint")
		{
			return "WriteUInt32LittleEndian";
		}
		else if (typeName == "long")
		{
			return "WriteInt64LittleEndian";
		}
		else if (typeName == "ulong")
		{
			return "WriteUInt64LittleEndian";
		}
		else
		{
			throw new NotSupportedException($"Type '{typeName}' is not supported for BinaryPrimitives serialization.");
		}
	}
}
