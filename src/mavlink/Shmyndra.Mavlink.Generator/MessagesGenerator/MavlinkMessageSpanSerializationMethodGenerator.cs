using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates Mavlink message serialization methods using the Span-based approach (with BinaryPrimitives).
/// </summary>
public class MavlinkMessageSpanSerializationMethodGenerator : MavlinkMessageSerializationMethodGeneratorBase
{
	public override GeneratedMavlinkMessageSerializeMethod CreateSerializeMethod(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var serializeWithoutExtensionsMethod = CreateSerializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		var serializeWithExtensionsMethod = fields.Any(x => !x.IsRequired)
			? CreateSerializeWithExtensionsMethodInternal(@namespace, messageName, fields)
			: null;
		return new GeneratedMavlinkMessageSerializeMethod(
			@namespace,
			messageName,
			fields,
			serializeWithoutExtensionsMethod,
			serializeWithExtensionsMethod);
	}

	internal override MethodDeclarationSyntax CreateSerializeWithoutExtensionsMethodInternal(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int minSize = fields.CalculateMinSize();
		methodBody.AppendLine($"byte[] buffer = new byte[{minSize}];");
		methodBody.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");

		int currentOffset = 0;
		var sortedFields = GetSortedFields(fields).requiredFields
			.Concat(GetSortedFields(fields).arrayFields)
			.ToList();

		currentOffset = AppendFields(methodBody, sortedFields, currentOffset, isExtension: false);

		methodBody.AppendLine("return buffer;");
		return WrapMethod("SerializeWithoutExtensions", methodBody.ToString());
	}

	internal override MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int finalSize = fields.CalculateFinalSize();
		methodBody.AppendLine($"byte[] buffer = new byte[{finalSize}];");
		methodBody.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");

		int currentOffset = 0;
		var sortedFields = GetSortedFields(fields).requiredFields
			.Concat(GetSortedFields(fields).arrayFields)
			.ToList();
		currentOffset = AppendFields(methodBody, sortedFields, currentOffset, isExtension: false);

		var extensionFields = fields.Where(f => !f.IsRequired).ToList();
		currentOffset = AppendFields(methodBody, extensionFields, currentOffset, isExtension: true);

		methodBody.AppendLine("return buffer;");
		return WrapMethod("SerializeWithExtensions", methodBody.ToString());
	}

	private int AppendFields(StringBuilder sb, IEnumerable<GeneratedMavlinkMessageField> fields, int startingOffset, bool isExtension)
	{
		int offset = startingOffset;
		foreach (var field in fields)
		{
			AppendFieldSerialization(sb, field, offset, isExtension);
			offset += field.GetFieldSize();
		}
		return offset;
	}

	private void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, int offset, bool isExtension)
	{
		var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);

		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			if (isExtension)
			{
				sb.AppendLine($"if ({fieldPropertyName}?.IsDefaultOrEmpty == false)");
				sb.AppendLine("{");
				sb.AppendLine(GenerateArraySerialization(fieldPropertyName, arrayType, offset, isRequired: false));
				sb.AppendLine("}");
			}
			else
			{
				sb.AppendLine(GenerateArraySerialization(fieldPropertyName, arrayType, offset, isRequired: true));
			}
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
		{
			if (isExtension)
			{
				sb.AppendLine($"if ({fieldPropertyName}.HasValue)");
				sb.AppendLine("{");
				sb.AppendLine(GenerateEnumSerialization(fieldPropertyName, enumType, offset, isRequired: false));
				sb.AppendLine("}");
			}
			else
			{
				sb.AppendLine(GenerateEnumSerialization(fieldPropertyName, enumType, offset, isRequired: true));
			}
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
		{
			if (isExtension)
			{
				sb.AppendLine($"if ({fieldPropertyName}?.IsDefaultOrEmpty == false)");
				sb.AppendLine("{");
				sb.AppendLine(GenerateArrayEnumSerialization(fieldPropertyName, arrayEnumType, offset, isRequired: false));
				sb.AppendLine("}");
			}
			else
			{
				sb.AppendLine(GenerateArrayEnumSerialization(fieldPropertyName, arrayEnumType, offset, isRequired: true));
			}
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleField)
		{
			if (isExtension)
			{
				sb.AppendLine($"if ({fieldPropertyName}.HasValue)");
				sb.AppendLine("{");
				sb.AppendLine(GenerateSimpleTypeSerialization(fieldPropertyName, simpleField.ConvertedType, offset, isRequired: false));
				sb.AppendLine("}");
			}
			else
			{
				sb.AppendLine(GenerateSimpleTypeSerialization(fieldPropertyName, simpleField.ConvertedType, offset, isRequired: true));
			}
		}
	}

	private static string GenerateArraySerialization(string variableName, GeneratedMavlinkMessageFieldArrayType arrayType, int offset, bool isRequired)
	{
		var elementType = arrayType.ConvertedType;
		int typeSize = Utilities.GetDotNetTypeSize(elementType);

		if (elementType == "byte")
		{
			return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    finalSpan[{offset} + i] = {variableName}[i];
}}";
		}
		else if (elementType == "sbyte")
		{
			return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    finalSpan[{offset} + i] = (byte){variableName}[i];
}}";
		}
		else if (elementType == "float")
		{
			return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
        finalSpan.Slice({offset} + i * {typeSize}, {typeSize}),
        BitConverter.SingleToInt32Bits({variableName}[i])
    );
}}";
		}
		else if (elementType == "double")
		{
			return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(
        finalSpan.Slice({offset} + i * {typeSize}, {typeSize}),
        BitConverter.DoubleToInt64Bits({variableName}[i])
    );
}}";
		}
		else if (elementType == "char")
		{
			return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), (ushort){variableName}[i]);
}}";
		}

		string writeMethod = elementType switch
		{
			"ushort" => "WriteUInt16LittleEndian",
			"uint" => "WriteUInt32LittleEndian",
			"int" => "WriteInt32LittleEndian",
			"short" => "WriteInt16LittleEndian",
			"long" => "WriteInt64LittleEndian",
			"ulong" => "WriteUInt64LittleEndian",
			_ => throw new NotSupportedException($"Array element type '{elementType}' is not supported for serialization.")
		};

		return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), {variableName}[i]);
}}";
	}

	private static string GenerateEnumSerialization(string variableName, GeneratedMavlinkMessageFieldEnumType enumType, int offset, bool isRequired)
	{
		return enumType.ConvertedType switch
		{
			"byte" => isRequired
						? $"finalSpan[{offset}] = (byte){variableName};"
						: $"finalSpan[{offset}] = (byte){variableName}.Value;",
			"sbyte" => isRequired
						? $"finalSpan[{offset}] = (sbyte){variableName};"
						: $"finalSpan[{offset}] = (sbyte){variableName}.Value;",
			"ushort" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName}.Value);",
			"short" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(finalSpan.Slice({offset}, 2), (short){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(finalSpan.Slice({offset}, 2), (short){variableName}.Value);",
			"uint" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), (uint){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), (uint){variableName}.Value);",
			"int" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), (int){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), (int){variableName}.Value);",
			"ulong" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), (ulong){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), (ulong){variableName}.Value);",
			"long" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), (long){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), (long){variableName}.Value);",
			_ => throw new NotSupportedException($"Enum type '{enumType.ConvertedType}' is not supported for serialization.")
		};
	}

	private static string GenerateArrayEnumSerialization(string variableName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, int offset, bool isRequired)
	{
		int typeSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);

		if (arrayEnumType.ConvertedType == "byte")
		{
			return $@"
for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)
{{
    finalSpan[{offset} + i] = (byte){variableName}[i];
}}";
		}
		else if (arrayEnumType.ConvertedType == "sbyte")
		{
			return $@"
for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)
{{
    finalSpan[{offset} + i] = (sbyte){variableName}[i];
}}";
		}

		string writeMethod = arrayEnumType.ConvertedType switch
		{
			"ushort" => "WriteUInt16LittleEndian",
			"uint" => "WriteUInt32LittleEndian",
			"short" => "WriteInt16LittleEndian",
			"int" => "WriteInt32LittleEndian",
			"long" => "WriteInt64LittleEndian",
			"ulong" => "WriteUInt64LittleEndian",
			_ => throw new NotSupportedException($"Array enum type '{arrayEnumType.ConvertedType}' is not supported for serialization.")
		};

		return $@"
for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.{writeMethod}(finalSpan.Slice({offset} + i * {typeSize}, {typeSize}), ({arrayEnumType.ConvertedType}){variableName}[i]);
}}";
	}

	private static string GenerateSimpleTypeSerialization(string variableName, string typeName, int offset, bool isRequired)
	{
		return typeName switch
		{
			"byte" => isRequired
						? $"finalSpan[{offset}] = {variableName};"
						: $"finalSpan[{offset}] = {variableName}.Value;",
			"sbyte" => isRequired
						? $"finalSpan[{offset}] = (byte){variableName};"
						: $"finalSpan[{offset}] = (byte){variableName}.Value;",
			"char" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), (ushort){variableName}.Value);",
			"float" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}));"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}.Value));",
			"double" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({variableName}));"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), BitConverter.DoubleToInt64Bits({variableName}.Value));",
			"short" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName}.Value);",
			"long" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName}.Value);",
			"ulong" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName}.Value);",
			"ushort" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName}.Value);",
			"uint" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName}.Value);",
			"int" => isRequired
						? $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});"
						: $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName}.Value);",
			_ => throw new NotSupportedException($"Simple type '{typeName}' is not supported for serialization.")
		};
	}
}
