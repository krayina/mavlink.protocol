using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBufferSerializationGeneratorStrategy : IMavlinkSerializationGeneratorStrategy
{
	private const int BitsPerByte = 8;

	public void AppendBufferInitialization(StringBuilder sb, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
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
			AppendRequiredSimpleField(sb, variableName, typeName, offset, size);
		}
		else
		{
			AppendOptionalSimpleField(sb, variableName, typeName, offset, size);
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string variableName)
	{
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(arrayType.ConvertedType);

		if (field.IsRequired)
		{
			AppendRequiredArrayField(sb, variableName, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayField(sb, variableName, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string variableName)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			AppendBitmaskArrayEnumField(sb, variableName, arrayEnumType.ArrayLength, elementSize, offset);
		}
		else if (field.IsRequired)
		{
			AppendRequiredArrayField(sb, variableName, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayField(sb, variableName, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendRequiredSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($"buffer[{offset}] = {variableName};");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"buffer[{offset}] = (byte){variableName};");
		}
		else
		{
			sb.AppendLine($"BitConverter.GetBytes({variableName}).CopyTo(buffer, {offset});");
		}
	}

	private void AppendOptionalSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = {variableName}.Value;
}}");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = (byte){variableName}.Value;
}}");
		}
		else
		{
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    BitConverter.GetBytes({variableName}.Value).CopyTo(buffer, {offset});
}}");
		}
	}

	private void AppendRequiredArrayField(StringBuilder sb, string variableName, int offset, int totalSize)
	{
		sb.AppendLine($"Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {totalSize});");
	}

	private void AppendOptionalArrayField(StringBuilder sb, string variableName, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({variableName}.Value.ToArray(), 0, buffer, {offset}, {totalSize});
}}");
	}

	private void AppendBitmaskEnumField(StringBuilder sb, string variableName, string typeName, int size, int offset)
	{
		string combinedType = Utilities.GetCombinedTypeForTotalBits(size * BitsPerByte);
		AppendBitmask(sb, variableName, combinedType, size);
		sb.AppendLine($"BitConverter.GetBytes(combined_{variableName}).CopyTo(buffer, {offset});");
	}

	private void AppendBitmaskArrayEnumField(StringBuilder sb, string variableName, int arrayLength, int elementSize, int offset)
	{
		string combinedType = Utilities.GetCombinedTypeForTotalBits(arrayLength * elementSize * BitsPerByte);
		AppendBitmask(sb, variableName, combinedType, elementSize);
		sb.AppendLine($"BitConverter.GetBytes(combined_{variableName}).CopyTo(buffer, {offset});");
	}

	private void AppendBitmask(StringBuilder sb, string variableName, string combinedType, int elementSize)
	{
		sb.AppendLine($"{combinedType} combined_{variableName} = 0;");
		sb.AppendLine($"for (int i = 0; i < {variableName}.Length; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    combined_{variableName} |= (({combinedType}){variableName}[i]) << (i * {elementSize * BitsPerByte});");
		sb.AppendLine("}");
	}
}
