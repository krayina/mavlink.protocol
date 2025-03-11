using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBitmaskFieldBufferSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	private const int BitsPerByte = 8;

	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace)
	{
		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendBitmaskEnumField(sb, field, enumType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendBitmaskArrayEnumField(sb, field, arrayEnumType, ref offset, variableName);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private void AppendBitmaskEnumField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string variableName)
	{
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string combinedType = Utilities.GetCombinedTypeForTotalBits(size * BitsPerByte);

		if (field.Original.IsRequired)
		{
			AppendBitmask(sb, variableName, combinedType, size);
			sb.AppendLine($"BitConverter.GetBytes(combined{variableName}).CopyTo(buffer, {offset});");
		}
		else
		{
			sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    {AppendBitmaskInline(variableName, combinedType, size)}
    BitConverter.GetBytes(combined{variableName}).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendBitmaskArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageField filed, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string variableName)
	{
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (filed.Original.IsRequired)
		{
			sb.AppendLine($"var serialized{variableName} = new {elementTypeName}[{arrayEnumType.ArrayLength}];");
			sb.AppendLine($"for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)");
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
		else
		{
			sb.AppendLine($@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    var serialized{variableName} = new {elementTypeName}[{arrayEnumType.ArrayLength}];
    for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)
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

		offset += totalSize;
	}

	private void AppendBitmask(StringBuilder sb, string variableName, string combinedType, int elementSize)
	{
		sb.AppendLine($"{combinedType} combined{variableName} = 0;");
		sb.AppendLine($"for (int i = 0; i < {variableName}.Length; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    combined{variableName} |= (({combinedType}){variableName}[i]) << (i * {elementSize * BitsPerByte});");
		sb.AppendLine("}");
	}

	private string AppendBitmaskInline(string variableName, string combinedType, int elementSize)
	{
		return $"{combinedType} combined{variableName} = 0;\n" +
			   $"for (int i = 0; i < {variableName}.Value.Length; i++)\n" +
			   "{\n" +
			   $"    combined{variableName} |= (({combinedType}){variableName}.Value[i]) << (i * {elementSize * BitsPerByte});\n" +
			   "}";
	}
}
