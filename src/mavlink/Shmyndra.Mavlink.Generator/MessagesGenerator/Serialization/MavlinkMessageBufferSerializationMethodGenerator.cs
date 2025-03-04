using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates Mavlink message serialization methods using the traditional buffer approach (BitConverter and Buffer.BlockCopy).
/// </summary>
public class MavlinkMessageBufferSerializationMethodGenerator : MavlinkMessageSerializationMethodGeneratorBase
{
	/// <summary>
	/// Appends the prologue for serialization, initializing the buffer with the required size.
	/// </summary>
	protected override void AppendMethodPrologue(StringBuilder sb, string messageName, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
	}

	/// <summary>
	/// Appends serialization logic for simple fields (e.g., byte, int, float).
	/// </summary>
	protected override void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName, bool isRequired)
	{
		string typeName = simpleType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);

		if (isRequired)
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"buffer[{offset}] = {varName};");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($"buffer[{offset}] = (byte){varName};");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes({varName}).CopyTo(buffer, {offset});");
			}
		}
		else
		{
			if (typeName == "byte")
			{
				sb.AppendLine($"buffer[{offset}] = {varName}.HasValue ? {varName}.Value : (byte)0;");
			}
			else if (typeName == "sbyte")
			{
				sb.AppendLine($"buffer[{offset}] = {varName}.HasValue ? (byte){varName}.Value : (byte)0;");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes({varName}.HasValue ? {varName}.Value : ({typeName})0).CopyTo(buffer, {offset});");
			}
		}

		offset += size;
	}

	/// <summary>
	/// Appends serialization logic for enum fields.
	/// </summary>
	protected override void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string varName, string currentNamespace, bool isRequired)
	{
		string convertedType = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(convertedType);

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = size * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);

			sb.AppendLine($"{combinedType} combined_{varName} = 0;");
			sb.AppendLine($"for (int i = 0; i < {varName}.Length; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    combined_{varName} |= (({combinedType}){varName}[i]) << (i * {size * 8});");
			sb.AppendLine("}");
			sb.AppendLine($"BitConverter.GetBytes(combined_{varName}).CopyTo(buffer, {offset});");
		}
		else if (isRequired)
		{
			if (size == 1)
			{
				sb.AppendLine($"buffer[{offset}] = ({convertedType}){varName};");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes(({convertedType}){varName}).CopyTo(buffer, {offset});");
			}
		}
		else
		{
			if (size == 1)
			{
				sb.AppendLine($"buffer[{offset}] = {varName}.HasValue ? ({convertedType}){varName}.Value : ({convertedType})0;");
			}
			else
			{
				sb.AppendLine($"BitConverter.GetBytes({varName}.HasValue ? ({convertedType}){varName}.Value : ({convertedType})0).CopyTo(buffer, {offset});");
			}
		}

		offset += size;
	}

	/// <summary>
	/// Appends serialization logic for array fields.
	/// </summary>
	protected override void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName, bool isRequired)
	{
		string elementType = arrayType.ConvertedType;
		int arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);

		if (isRequired)
		{
			sb.AppendLine($"Buffer.BlockCopy({varName}.ToArray(), 0, buffer, {offset}, {arrayLength});");
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue && !{varName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({varName}.Value.ToArray(), 0, buffer, {offset}, {arrayLength});
}}
else
{{
    for (int i = {offset}; i < {offset} + {arrayLength}; i++)
    {{
        buffer[i] = 0;
    }}
}}");
		}

		offset += arrayLength;
	}

	/// <summary>
	/// Appends serialization logic for array of enum fields.
	/// </summary>
	protected override void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string varName, bool isRequired)
	{
		string elementType = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementType);
		int arrayLength = arrayEnumType.ArrayLength * elementSize;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = arrayEnumType.ArrayLength * elementSize * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);

			sb.AppendLine($"{combinedType} combined_{varName} = 0;");
			sb.AppendLine($"for (int i = 0; i < {varName}.Length; i++)");
			sb.AppendLine("{");
			sb.AppendLine($"    combined_{varName} |= (({combinedType}){varName}[i]) << (i * {elementSize * 8});");
			sb.AppendLine("}");
			sb.AppendLine($"BitConverter.GetBytes(combined_{varName}).CopyTo(buffer, {offset});");
		}
		else if (isRequired)
		{
			sb.AppendLine($"Buffer.BlockCopy({varName}.ToArray(), 0, buffer, {offset}, {arrayLength});");
		}
		else
		{
			sb.AppendLine($@"
if ({varName}.HasValue && !{varName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({varName}.Value.ToArray(), 0, buffer, {offset}, {arrayLength});
}}
else
{{
    for (int i = {offset}; i < {offset} + {arrayLength}; i++)
    {{
        buffer[i] = 0;
    }}
}}");
		}

		offset += arrayLength;
	}

	/// <summary>
	/// Returns the appropriate combined type based on the total number of bits required.
	/// </summary>
	private static string GetCombinedTypeForTotalBits(int totalBits)
	{
		if (totalBits <= 8)
		{
			return "byte";
		}
		else if (totalBits <= 16)
		{
			return "ushort";
		}
		else if (totalBits <= 32)
		{
			return "uint";
		}
		else
		{
			return "ulong";
		}
	}
}
