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
			if (typeName == "byte")
			{
				sb.AppendLine($"buffer[{offset}] = ({typeName}){variableName};");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($"buffer[{offset}] = (byte)({typeName}){variableName};");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes(({typeName}){variableName}).CopyTo(buffer, {offset});");
			}
		}
		else
		{
			if (typeName == "byte")
			{
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = ({typeName}){variableName}.Value;
}}");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = (byte)({typeName}){variableName}.Value;
}}");
			}
			else
			{
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    BitConverter.GetBytes(({typeName}){variableName}.Value).CopyTo(buffer, {offset});
}}");
			}
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

	private void AppendRequiredArrayEnumField(StringBuilder sb, string variableName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($"var serialized{variableName} = new {elementTypeName}[{arrayLength}];");
		sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    {elementTypeName} combinedFlags = 0;");
		sb.AppendLine($"    foreach (var flag in {variableName}[i])");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        combinedFlags |= ({elementTypeName})flag;");
		sb.AppendLine($"    }}");
		sb.AppendLine($"    serialized{variableName}[i] = combinedFlags;");
		sb.AppendLine("}");
		sb.AppendLine($"Buffer.BlockCopy(serialized{variableName}, 0, buffer, {offset}, {totalSize});");
	}

	private void AppendOptionalArrayEnumField(StringBuilder sb, string variableName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    var serialized{variableName} = new {elementTypeName}[{arrayLength}];
    for (int i = 0; i < {arrayLength}; i++)
    {{
        {elementTypeName} combinedFlags = 0;
        foreach (var flag in {variableName}.Value[i])
        {{
            combinedFlags |= ({elementTypeName})flag;
        }}
        serialized{variableName}[i] = combinedFlags;
    }}
    Buffer.BlockCopy(serialized{variableName}, 0, buffer, {offset}, {totalSize});
}}");
	}

	private void AppendBitmaskEnumField(StringBuilder sb, string variableName, string typeName, int size, int offset)
	{
		string combinedType = Utilities.GetCombinedTypeForTotalBits(size * BitsPerByte);
		AppendBitmask(sb, variableName, combinedType, size);
		sb.AppendLine($"BitConverter.GetBytes(combined{variableName}).CopyTo(buffer, {offset});");
	}

	private void AppendBitmask(StringBuilder sb, string variableName, string combinedType, int elementSize)
	{
		sb.AppendLine($"{combinedType} combined{variableName} = 0;");
		sb.AppendLine($"for (int i = 0; i < {variableName}.Length; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    combined{variableName} |= (({combinedType}){variableName}[i]) << (i * {elementSize * BitsPerByte});");
		sb.AppendLine("}");
	}
}
