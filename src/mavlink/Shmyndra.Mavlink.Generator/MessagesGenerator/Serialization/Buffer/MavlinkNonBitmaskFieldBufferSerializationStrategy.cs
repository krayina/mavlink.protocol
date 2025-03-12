using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkNonBitmaskFieldBufferSerializationStrategy : IMavlinkFieldSerializationStrategy
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
				sb.AppendLine($"buffer[{offset}] = ({typeName}){propertyName};");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($"buffer[{offset}] = (byte)({typeName}){propertyName};");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes(({typeName}){propertyName}).CopyTo(buffer, {offset});");
			}
		}
		else
		{
			if (typeName == "byte")
			{
				sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    buffer[{offset}] = ({typeName}){propertyName}.Value;
}}");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    buffer[{offset}] = (byte)({typeName}){propertyName}.Value;
}}");
			}
			else
			{
				sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    BitConverter.GetBytes(({typeName}){propertyName}.Value).CopyTo(buffer, {offset});
}}");
			}
		}

		offset += size;
	}

	private void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		int totalSize = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(arrayType.ConvertedType);

		if (field.Original.IsRequired)
		{
			AppendRequiredArrayField(sb, propertyName, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayField(sb, propertyName, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset)
	{
		string propertyName = field.GeneratedName;
		string serializedVarName = $"serialized{field.GeneratedName}";
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		if (field.Original.IsRequired)
		{
			AppendRequiredArrayEnumField(sb, propertyName, serializedVarName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}
		else
		{
			AppendOptionalArrayEnumField(sb, propertyName, serializedVarName, elementTypeName, arrayEnumType.ArrayLength, offset, totalSize);
		}

		offset += totalSize;
	}

	private void AppendRequiredSimpleField(StringBuilder sb, string propertyName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($"buffer[{offset}] = {propertyName};");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"buffer[{offset}] = (byte){propertyName};");
		}
		else
		{
			sb.AppendLine($"BitConverter.GetBytes({propertyName}).CopyTo(buffer, {offset});");
		}
	}

	private void AppendOptionalSimpleField(StringBuilder sb, string propertyName, string typeName, int offset, int size)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    buffer[{offset}] = {propertyName}.Value;
}}");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    buffer[{offset}] = (byte){propertyName}.Value;
}}");
		}
		else
		{
			sb.AppendLine($@"
if ({propertyName}.HasValue)
{{
    BitConverter.GetBytes({propertyName}.Value).CopyTo(buffer, {offset});
}}");
		}
	}

	private void AppendRequiredArrayField(StringBuilder sb, string propertyName, int offset, int totalSize)
	{
		sb.AppendLine($"Buffer.BlockCopy({propertyName}.ToArray(), 0, buffer, {offset}, {totalSize});");
	}

	private void AppendOptionalArrayField(StringBuilder sb, string propertyName, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({propertyName}.Value.ToArray(), 0, buffer, {offset}, {totalSize});
}}");
	}

	private void AppendRequiredArrayEnumField(StringBuilder sb, string propertyName, string serializedVarName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($"var {serializedVarName} = new {elementTypeName}[{arrayLength}];");
		sb.AppendLine($"for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("{");
		sb.AppendLine($"    {elementTypeName} combinedFlags = 0;");
		sb.AppendLine($"    foreach (var flag in {propertyName}[i])");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        combinedFlags |= ({elementTypeName})flag;");
		sb.AppendLine($"    }}");
		sb.AppendLine($"    {serializedVarName}[i] = combinedFlags;");
		sb.AppendLine("}");
		sb.AppendLine($"Buffer.BlockCopy({serializedVarName}, 0, buffer, {offset}, {totalSize});");
	}

	private void AppendOptionalArrayEnumField(StringBuilder sb, string propertyName, string serializedVarName, string elementTypeName, int arrayLength, int offset, int totalSize)
	{
		sb.AppendLine($@"
if ({propertyName}.HasValue && !{propertyName}.Value.IsDefaultOrEmpty)
{{
    var {serializedVarName} = new {elementTypeName}[{arrayLength}];
    for (int i = 0; i < {arrayLength}; i++)
    {{
        {elementTypeName} combinedFlags = 0;
        foreach (var flag in {propertyName}.Value[i])
        {{
            combinedFlags |= ({elementTypeName})flag;
        }}
        {serializedVarName}[i] = combinedFlags;
    }}
    Buffer.BlockCopy({serializedVarName}, 0, buffer, {offset}, {totalSize});
}}");
	}
}
