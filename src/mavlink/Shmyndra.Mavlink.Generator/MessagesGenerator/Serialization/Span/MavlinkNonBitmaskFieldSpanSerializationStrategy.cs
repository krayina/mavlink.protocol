using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class NonBitmaskFieldSpanSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace)
	{
		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendEnumFieldSerialization(sb, field, enumType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayFieldSerialization(sb, field, arrayType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendArrayEnumFieldSerialization(sb, field, arrayEnumType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				AppendSimpleFieldSerialization(sb, field, simpleType, ref offset, variableName);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Non-Bitmask strategy.");
		}
	}

	private void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldPrimitiveType simpleType, ref int offset, string variableName)
	{
		string typeName = simpleType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
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

		if (field.Original.IsRequired)
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = ({typeName}){variableName};");
			}
			else
			{
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
					.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){variableName});");
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
    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
		.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), ({typeName}){variableName}.Value);
}}");
			}
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string variableName)
	{
		string typeName = arrayType.ConvertedType;
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
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

		if (field.Original.IsRequired)
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
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
				.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {variableName});");
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
    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
		.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset}, {size}), {variableName}.Value);
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
		sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
			.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);");
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
        System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
			.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {totalSize / arrayLength}, {totalSize / arrayLength}), combinedFlags);
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
			return $"for (int i = 0; i < {arrayLength}; i++) {{ System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions
				.GetBinaryPrimitivesWriteMethod(typeName)}(finalSpan.Slice({offset} + i * {size}, {size}), {arrayName}[i]); }}";
		}
	}
}
