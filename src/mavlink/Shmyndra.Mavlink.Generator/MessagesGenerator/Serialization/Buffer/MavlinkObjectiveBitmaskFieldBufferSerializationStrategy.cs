using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldBufferSerializationStrategy : IMavlinkFieldSerializationStrategy
{
	public void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		if (field.Original.Display != MavlinkMessageFieldDisplay.Bitmask)
		{
			throw new NotSupportedException("Objective Bitmask strategy supports only bitmask fields.");
		}

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldPrimitiveType primitiveType:
				AppendPrimitiveBitmaskField(sb, field, primitiveType, ref offset);
				break;
			case GeneratedMavlinkMessageFieldEnumType enumType:
				AppendEnumBitmaskField(sb, field, enumType, ref offset);
				break;
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayBitmaskField(sb, field, arrayType, ref offset);
				break;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Objective Bitmask strategy.");
		}
	}

	private void AppendPrimitiveBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldPrimitiveType primitiveType, ref int offset)
	{
		int size = field.GetFieldSize();
		string propertyName = field.GeneratedName;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"BitConverter.GetBytes({propertyName}.Bitmask).CopyTo(buffer, {offset});");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    BitConverter.GetBytes({propertyName}.Value.Bitmask).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendEnumBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset)
	{
		int size = field.GetFieldSize();
		string propertyName = field.GeneratedName;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"BitConverter.GetBytes({propertyName}.Bitmask).CopyTo(buffer, {offset});");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    BitConverter.GetBytes({propertyName}.Value.Bitmask).CopyTo(buffer, {offset});
}}");
		}

		offset += size;
	}

	private void AppendArrayBitmaskField(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset)
	{
		int totalSize = field.GetFieldSize();
		string propertyName = field.GeneratedName;
		string serializedVarName = $"serialized{field.GeneratedName}";
		string arrayLength = arrayType.ArrayLength.ToString();
		string elementType = arrayType.ConvertedType;

		if (field.Original.IsRequired)
		{
			sb.AppendLine($"var {serializedVarName} = new {elementType}[{arrayLength}];");
			sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    {serializedVarName}[i] = {propertyName}[i].Bitmask;");
			sb.AppendLine("}");
			sb.AppendLine($"Buffer.BlockCopy({serializedVarName}, 0, buffer, {offset}, {totalSize});");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    var {serializedVarName} = new {elementType}[{arrayLength}];
    for (int i = 0; i < {arrayLength}; i++)
    {{
        {serializedVarName}[i] = {propertyName}.Value[i].Bitmask;
    }}
    Buffer.BlockCopy({serializedVarName}, 0, buffer, {offset}, {totalSize});
}}");
		}

		offset += totalSize;
	}
}
