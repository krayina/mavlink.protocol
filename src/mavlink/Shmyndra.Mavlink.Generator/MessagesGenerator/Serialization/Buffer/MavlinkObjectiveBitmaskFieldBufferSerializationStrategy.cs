using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldBufferSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace)
	{
		if (field.Original.Display != MavlinkMessageFieldDisplay.Bitmask)
		{
			throw new NotSupportedException("Objective Bitmask strategy supports only bitmask fields.");
		}

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldPrimitiveType primitiveType:
				AppendPrimitiveBitmaskField(sb, field, primitiveType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldEnumType enumType:
				AppendEnumBitmaskField(sb, field, enumType, ref offset, variableName, currentNamespace);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayBitmaskField(sb, field, arrayType, ref offset, variableName);
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				AppendArrayEnumBitmaskField(sb, field, arrayEnumType, ref offset, variableName, currentNamespace);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Objective Bitmask strategy.");
		}
	}

	private void AppendPrimitiveBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldPrimitiveType primitiveType, ref int offset, string variableName)
	{
		int size = field.GetFieldSize();
		string safeVarName = Utilities.GetSafeVariableName(variableName);

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"BitConverter.GetBytes({safeVarName}.Bitmask).CopyTo(buffer, {offset});");
		}
		else
		{
			sb.AppendLine($@"
if ({safeVarName}.HasValue)
{{
    BitConverter.GetBytes({safeVarName}.Value.Bitmask).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendEnumBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string variableName, string currentNamespace)
	{
		int size = field.GetFieldSize();
		string safeVarName = Utilities.GetSafeVariableName(variableName);

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"BitConverter.GetBytes({safeVarName}.Bitmask).CopyTo(buffer, {offset});");
		}
		else
		{
			sb.AppendLine($@"
if ({safeVarName}.HasValue)
{{
    BitConverter.GetBytes({safeVarName}.Value.Bitmask).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendArrayBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string variableName)
	{
		int totalSize = field.GetFieldSize();
		string safeVarName = Utilities.GetSafeVariableName(variableName);
		string arrayLength = arrayType.ArrayLength.ToString();
		string elementType = arrayType.ConvertedType;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"var serialized{safeVarName} = new {elementType}[{arrayLength}];");
			sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    serialized{safeVarName}[i] = {safeVarName}[i].Bitmask;");
			sb.AppendLine("}");
			sb.AppendLine($"Buffer.BlockCopy(serialized{safeVarName}, 0, buffer, {offset}, {totalSize});");
		}
		else
		{
			sb.AppendLine($@"
if ({safeVarName}.HasValue && !{safeVarName}.Value.IsDefaultOrEmpty)
{{
    var serialized{safeVarName} = new {elementType}[{arrayLength}];
    for (int i = 0; i < {arrayLength}; i++)
    {{
        serialized{safeVarName}[i] = {safeVarName}.Value[i].Bitmask;
    }}
    Buffer.BlockCopy(serialized{safeVarName}, 0, buffer, {offset}, {totalSize});
}}");
		}

		offset += totalSize;
	}

	private void AppendArrayEnumBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string variableName, string currentNamespace)
	{
		int totalSize = field.GetFieldSize();
		string safeVarName = Utilities.GetSafeVariableName(variableName);
		string arrayLength = arrayEnumType.ArrayLength.ToString();
		string elementType = arrayEnumType.ConvertedType;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"var serialized{safeVarName} = new {elementType}[{arrayLength}];");
			sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    serialized{safeVarName}[i] = {safeVarName}[i].Bitmask;");
			sb.AppendLine("}");
			sb.AppendLine($"Buffer.BlockCopy(serialized{safeVarName}, 0, buffer, {offset}, {totalSize});");
		}
		else
		{
			sb.AppendLine($@"
if ({safeVarName}.HasValue && !{safeVarName}.Value.IsDefaultOrEmpty)
{{
    var serialized{safeVarName} = new {elementType}[{arrayLength}];
    for (int i = 0; i < {arrayLength}; i++)
    {{
        serialized{safeVarName}[i] = {safeVarName}.Value[i].Bitmask;
    }}
    Buffer.BlockCopy(serialized{safeVarName}, 0, buffer, {offset}, {totalSize});
}}");
		}

		offset += totalSize;
	}
}
