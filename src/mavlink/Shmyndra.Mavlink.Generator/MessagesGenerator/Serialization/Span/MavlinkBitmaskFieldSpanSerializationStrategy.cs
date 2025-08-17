using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class BitmaskFieldSpanSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	private const int BitsPerByte = 8;

	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType
				when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendBitmaskEnumField(sb, field, enumType, ref offset);
				break;

			case GeneratedMavlinkMessageFieldArrayType { ElementType: GeneratedMavlinkMessageFieldEnumType } arrayType
				when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendBitmaskArrayEnumField(sb, field, arrayType, ref offset);
				break;

			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private void AppendBitmaskEnumField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string combinedType = Utilities.GetCombinedTypeForTotalBits(size * BitsPerByte);

		if (field.Original.IsRequired)
		{
			AppendBitmask(sb, propertyName, combinedType, size);
			sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(combinedType)}(finalSpan.Slice({offset}, {size}), combined{propertyName});");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    {AppendBitmaskInline(propertyName, combinedType, size)}
    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(combinedType)}(finalSpan.Slice({offset}, {size}), combined{propertyName});
}}");
		}

		offset += size;
	}

	private void AppendBitmaskArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string elementTypeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayType.ArrayLength * elementSize;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"for (int i = 0; i < {arrayType.ArrayLength}; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    {elementTypeName} combinedFlags = 0;");
			sb.AppendLine($"    foreach (var flag in {propertyName}[i])");
			sb.AppendLine($"    {{");
			sb.AppendLine($"        combinedFlags |= ({elementTypeName})flag;");
			sb.AppendLine($"    }}");
			sb.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {elementSize}, {elementSize}), combinedFlags);");
			sb.AppendLine("}");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    for (int i = 0; i < {arrayType.ArrayLength}; i++)
    {{
        {elementTypeName} combinedFlags = 0;
        foreach (var flag in {propertyName}.Value[i])
        {{
            combinedFlags |= ({elementTypeName})flag;
        }}
        System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanSerializationExtensions.GetBinaryPrimitivesWriteMethod(elementTypeName)}(finalSpan.Slice({offset} + i * {elementSize}, {elementSize}), combinedFlags);
    }}
}}");
		}

		offset += totalSize;
	}

	private void AppendBitmask(StringBuilder sb, string propertyName, string combinedType, int elementSize)
	{
		sb.AppendLine($"{combinedType} combined{propertyName} = 0;");
		sb.AppendLine($"for (int i = 0; i < {propertyName}.Length; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    combined{propertyName} |= (({combinedType}){propertyName}[i]) << (i * {elementSize * BitsPerByte});");
		sb.AppendLine("}");
	}

	private string AppendBitmaskInline(string propertyName, string combinedType, int elementSize)
	{
		return $"{combinedType} combined{propertyName} = 0;\n" +
			   $"for (int i = 0; i < {propertyName}.Value.Length; i++)\n" +
			   "{\n" +
			   $"    combined{propertyName} |= (({combinedType}){propertyName}.Value[i]) << (i * {elementSize * BitsPerByte});\n" +
			   "}";
	}
}
