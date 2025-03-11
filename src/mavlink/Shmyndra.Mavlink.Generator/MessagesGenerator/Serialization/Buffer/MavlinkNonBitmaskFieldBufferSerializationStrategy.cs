using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkNonBitmaskFieldBufferSerializationStrategy : IMavlinkFieldSerializationStrategy
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
			AppendRequiredSimpleField(sb, variableName, typeName, offset, size);
		else
			AppendOptionalSimpleField(sb, variableName, typeName, offset, size);

		offset += size;
	}

	private void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string variableName)
	{
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (field.Original.IsRequired)
		{
			if (typeName == "byte")
				sb.AppendLine($"buffer[{offset}] = ({typeName}){variableName};");
			else if (typeName == "sbyte")
				sb.AppendLine($"buffer[{offset}] = (byte)({typeName}){variableName};");
			else
				sb.AppendLine($"BitConverter.GetBytes(({typeName}){variableName}).CopyTo(buffer, {offset});");
		}
		else
		{
			if (typeName == "byte")
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = ({typeName}){variableName}.Value;
}}");
			else if (typeName == "sbyte")
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = (byte)({typeName}){variableName}.Value;
}}");
			else
				sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    BitConverter.GetBytes(({typeName}){variableName}.Value).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string variableName)
	{
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(arrayType.ConvertedType);

		if (field.Original.IsRequired)
			AppendRequiredArrayField(sb, variableName, offset, totalSize);
		else
			AppendOptionalArrayField(sb, variableName, offset, totalSize);

		offset += totalSize;
	}

	private void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string variableName)
	{
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.Original.IsRequired)
			AppendRequiredArrayEnumField(sb, variableName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		else
			AppendOptionalArrayEnumField(sb, variableName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);

		offset += totalSize;
	}

	private void AppendRequiredSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
			sb.AppendLine($"buffer[{offset}] = {variableName};");
		else if (typeName == "sbyte")
			sb.AppendLine($"buffer[{offset}] = (byte){variableName};");
		else
			sb.AppendLine($"BitConverter.GetBytes({variableName}).CopyTo(buffer, {offset});");
	}

	private void AppendOptionalSimpleField(StringBuilder sb, string variableName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = {variableName}.Value;
}}");
		else if (typeName == "sbyte")
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = (byte){variableName}.Value;
}}");
		else
			sb.AppendLine($@"
if ({variableName}.HasValue)
{{
    BitConverter.GetBytes({variableName}.Value).CopyTo(buffer, {offset});
}}");
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
}
