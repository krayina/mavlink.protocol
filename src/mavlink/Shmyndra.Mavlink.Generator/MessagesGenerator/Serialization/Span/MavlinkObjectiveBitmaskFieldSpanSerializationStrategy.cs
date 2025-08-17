using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldSpanSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		if (field.Original.Display != MavlinkMessageFieldDisplay.Bitmask)
		{
			throw new NotSupportedException("Objective Bitmask strategy supports only bitmask fields.");
		}

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldScalarType scalarType:
				SerializeScalarBitmask(sb, field.GeneratedName, scalarType.ConvertedType, field.Original.IsRequired, ref offset);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				SerializeArrayBitmask(sb, field.GeneratedName, arrayType.ConvertedType, arrayType.ArrayLength, field.Original.IsRequired, ref offset);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Objective Bitmask strategy.");
		}
	}

	private void SerializeScalarBitmask(StringBuilder sb, string propertyName, string primitiveType, bool isRequired, ref int offset)
	{
		int size = Utilities.GetDotNetTypeSize(primitiveType);

		if (isRequired)
		{
			if (primitiveType == "byte")
			{
				sb.AppendLine($"finalSpan[{offset}] = {propertyName}.Bitmask;");
			}
			else
			{
				string writeMethod = MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(primitiveType);
				sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {size}), {propertyName}.Bitmask);");
			}
		}
		else
		{
			sb.AppendLine($"if ({propertyName}.HasValue)");
			sb.AppendLine("{");
			if (primitiveType == "byte")
			{
				sb.AppendLine($"    finalSpan[{offset}] = {propertyName}.Value.Bitmask;");
			}
			else
			{
				string writeMethod = MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(primitiveType);
				sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset}, {size}), {propertyName}.Value.Bitmask);");
			}
			sb.AppendLine("}");
		}

		offset += size;
	}

	private void SerializeArrayBitmask(StringBuilder sb, string propertyName, string primitiveType, int arrayLength, bool isRequired, ref int offset)
	{
		int elementSize = Utilities.GetDotNetTypeSize(primitiveType);

		if (isRequired)
		{
			sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
			sb.AppendLine("{");
			if (primitiveType == "byte")
			{
				sb.AppendLine($"    finalSpan[{offset} + i] = {propertyName}[i].Bitmask;");
			}
			else
			{
				string writeMethod = MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(primitiveType);
				sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset} + i * {elementSize}, {elementSize}), {propertyName}[i].Bitmask);");
			}
			sb.AppendLine("}");
		}
		else
		{
			sb.AppendLine($"if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)");
			sb.AppendLine("{");
			sb.AppendLine($"    for (int i = 0; i < {arrayLength}; i++)");
			sb.AppendLine("    {");
			if (primitiveType == "byte")
			{
				sb.AppendLine($"        finalSpan[{offset} + i] = {propertyName}.Value[i].Bitmask;");
			}
			else
			{
				string writeMethod = MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(primitiveType);
				sb.AppendLine($"        System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset} + i * {elementSize}, {elementSize}), {propertyName}.Value[i].Bitmask);");
			}
			sb.AppendLine("    }");
			sb.AppendLine("}");
		}

		offset += arrayLength * elementSize;
	}
}
