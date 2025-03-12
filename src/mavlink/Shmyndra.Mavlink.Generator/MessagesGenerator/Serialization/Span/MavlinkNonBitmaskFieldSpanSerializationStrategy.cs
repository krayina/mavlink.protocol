using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class NonBitmaskFieldSpanSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendEnumFieldSerialization(sb, field, enumType, ref offset);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayFieldSerialization(sb, field, arrayType, ref offset);
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendArrayEnumFieldSerialization(sb, field, arrayEnumType, ref offset);
				break;
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				AppendSimpleFieldSerialization(sb, field, simpleType, ref offset);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Non-Bitmask strategy.");
		}
	}

	private void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldPrimitiveType simpleType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string typeName = simpleType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
		{
			AppendRequiredSimpleField(sb, propertyName, typeName, offset, size);
		}
		else
		{
			AppendOptionalSimpleField(sb, propertyName, typeName, offset, size);
		}

		offset += size;
	}

	private void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = ({typeName}){propertyName};");
			}
			else
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){propertyName});");
			}
		}
		else
		{
			if (typeName == "byte")
			{
				sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    finalSpan[{offset}] = ({typeName}){propertyName}.Value;
}}");
			}
			else
			{
				sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){propertyName}.Value);
}}");
			}
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string typeName = arrayType.ConvertedType;
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
		{
			AppendRequiredArrayField(sb, propertyName, typeName, arrayType.ArrayLength, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayField(sb, propertyName, typeName, arrayType.ArrayLength, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.Original.IsRequired)
		{
			AppendRequiredArrayEnumField(sb, propertyName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayEnumField(sb, propertyName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendRequiredSimpleField(StringBuilder sb, string propertyName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($"finalSpan[{offset}] = {propertyName};");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"finalSpan[{offset}] = (byte){propertyName};");
		}
		else if (typeName == "char")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){propertyName});");
		}
		else if (typeName == "float")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({propertyName}));");
		}
		else if (typeName == "double")
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({propertyName}));");
		}
		else
		{
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {propertyName});");
		}
	}

	private void AppendOptionalSimpleField(StringBuilder sb, string propertyName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    finalSpan[{offset}] = {propertyName}.Value;
}}");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    finalSpan[{offset}] = (byte){propertyName}.Value;
}}");
		}
		else if (typeName == "char")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){propertyName}.Value);
}}");
		}
		else if (typeName == "float")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({propertyName}.Value));
}}");
		}
		else if (typeName == "double")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({propertyName}.Value));
}}");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {propertyName}.Value);
}}");
		}
	}

	private void AppendRequiredArrayField(StringBuilder sb, string propertyName, string typeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine(GenerateArraySerialization(propertyName, typeName, arrayLength, offset));
	}

	private void AppendOptionalArrayField(StringBuilder sb, string propertyName, string typeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    {GenerateArraySerialization($"{propertyName}.Value", typeName, arrayLength, offset)}
}}");
	}

	private void AppendRequiredArrayEnumField(StringBuilder sb, string propertyName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    {elementTypeName} combinedFlags = 0;");
		sb.AppendLine($"    foreach (var flag in {propertyName}[i])");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        combinedFlags |= ({elementTypeName})flag;");
		sb.AppendLine($"    }}");
		sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);");
		sb.AppendLine("}");
	}

	private void AppendOptionalArrayEnumField(StringBuilder sb, string propertyName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    for (int i = 0; i < {arrayLength}; i++)
    {{
        {elementTypeName} combinedFlags = 0;
        foreach (var flag in {propertyName}.Value[i])
        {{
            combinedFlags |= ({elementTypeName})flag;
        }}
        System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);
    }}
}}");
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
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset} + i * {size}, {size}), BitConverter.SingleToInt32Bits({arrayName}[i])); }}";
		}
		else if (typeName == "double")
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset} + i * {size}, {size}), BitConverter.DoubleToInt64Bits({arrayName}[i])); }}";
		}
		else
		{
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset} + i * {size}, {size}), {arrayName}[i]); }}";
		}
	}
}
