using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator.MessagesGenerator;

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

	internal override MethodDeclarationSyntax CreateSerializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		var minSize = fields.CalculateMinSize();
		methodBody.AppendLine($"byte[] buffer = new byte[{minSize}];");
		methodBody.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");

		int currentOffset = 0;
		var (requiredFields, arrayFields) = GetSortedFields(fields);
		var sortedFields = requiredFields.Concat(arrayFields).ToList();
		foreach (var field in sortedFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArraySerialization(fieldPropertyName, arrayType, currentOffset, true));
			}
			else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumSerialization(fieldPropertyName, enumType, currentOffset));
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumSerialization(fieldPropertyName, arrayEnumType, currentOffset, true));
			}
			else
			{
				var fieldType = ((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType;
				methodBody.AppendLine(GenerateSimpleTypeSerialization(fieldPropertyName, fieldType, currentOffset, true));
			}
			currentOffset += field.GetFieldSize();
		}
		methodBody.AppendLine("return buffer;");
		return WrapMethod("SerializeWithoutExtensions", methodBody.ToString());
	}

	internal override MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		var minSize = fields.CalculateMinSize();
		var extensionNonArrayFields = fields.Where(f => !f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).ToList();
		int extensionLength = extensionNonArrayFields.Sum(f => f.GetFieldSize());
		int arrayExtensionSize = fields.Where(f => !f.IsRequired && (f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).Sum(f => f.GetFieldSize());
		int finalSize = minSize + extensionLength + arrayExtensionSize;
		methodBody.AppendLine($"byte[] buffer = new byte[{finalSize}];");
		methodBody.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");

		int currentOffset = 0;
		var (requiredFields, arrayFields) = GetSortedFields(fields);
		var sortedFields = requiredFields.Concat(arrayFields).ToList();
		foreach (var field in sortedFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArraySerialization(fieldPropertyName, arrayType, currentOffset, true));
			}
			else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumSerialization(fieldPropertyName, enumType, currentOffset));
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumSerialization(fieldPropertyName, arrayEnumType, currentOffset, true));
			}
			else
			{
				var fieldType = ((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType;
				methodBody.AppendLine(GenerateSimpleTypeSerialization(fieldPropertyName, fieldType, currentOffset, true));
			}
			currentOffset += field.GetFieldSize();
		}

		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			int fieldSize = field.GetFieldSize();

			if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine($"if (!{fieldPropertyName}.IsDefaultOrEmpty)");
				methodBody.AppendLine("{");
				methodBody.AppendLine(GenerateArraySerialization(fieldPropertyName, arrayType, currentOffset, false));
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine($"if (!{fieldPropertyName}.IsDefaultOrEmpty)");
				methodBody.AppendLine("{");
				methodBody.AppendLine(GenerateArrayEnumSerialization(fieldPropertyName, arrayEnumType, currentOffset, false));
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldType simpleField && simpleField.ConvertedType == "byte")
			{
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    finalSpan[{currentOffset}] = {fieldPropertyName}.Value;");
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				string convertedType = enumType.ConvertedType;
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				switch (convertedType)
				{
					case "byte":
					case "sbyte":
						methodBody.AppendLine($"    finalSpan[{currentOffset}] = (byte){fieldPropertyName}.Value;");
						break;
					case "ushort":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({currentOffset}, 2), (ushort){fieldPropertyName}.Value);");
						break;
					case "uint":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), (uint){fieldPropertyName}.Value);");
						break;
					case "int":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), (int){fieldPropertyName}.Value);");
						break;
					case "ulong":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({currentOffset}, 8), (ulong){fieldPropertyName}.Value);");
						break;
					case "float":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), BitConverter.SingleToInt32Bits({fieldPropertyName}.Value));");
						break;
					default:
						methodBody.AppendLine($"    BitConverter.GetBytes(({convertedType}){fieldPropertyName}.Value).CopyTo(finalSpan.Slice({currentOffset}, {fieldSize}));");
						break;
				}
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldType simpleField2)
			{
				string convertedType = simpleField2.ConvertedType;
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				switch (convertedType)
				{
					case "byte":
						methodBody.AppendLine($"    finalSpan[{currentOffset}] = {fieldPropertyName}.Value;");
						break;
					case "sbyte":
						methodBody.AppendLine($"    finalSpan[{currentOffset}] = (byte){fieldPropertyName}.Value;");
						break;
					case "float":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), BitConverter.SingleToInt32Bits({fieldPropertyName}.Value));");
						break;
					case "ulong":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({currentOffset}, 8), {fieldPropertyName}.Value);");
						break;
					case "ushort":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({currentOffset}, 2), {fieldPropertyName}.Value);");
						break;
					case "uint":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), {fieldPropertyName}.Value);");
						break;
					case "int":
						methodBody.AppendLine($"    System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({currentOffset}, 4), {fieldPropertyName}.Value);");
						break;
					default:
						methodBody.AppendLine($"    BitConverter.GetBytes({fieldPropertyName}.Value).CopyTo(finalSpan.Slice({currentOffset}, {fieldSize}));");
						break;
				}
				methodBody.AppendLine("}");
			}
			currentOffset += fieldSize;
		}
		methodBody.AppendLine("return buffer;");
		return WrapMethod("SerializeWithExtensions", methodBody.ToString());
	}

	private static string GenerateArraySerialization(string variableName, GeneratedMavlinkMessageFieldArrayType arrayType, int offset, bool isRequired)
	{
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);
		return $@"
for (int i = 0; i < {arrayType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.Write{(elementType == "ushort" ? "UInt16" :
													  elementType == "uint" ? "UInt32" :
													  elementType == "int" ? "Int32" :
													  elementType == "short" ? "Int16" :
													  elementType == "long" ? "Int64" :
													  elementType == "ulong" ? "UInt64" : throw new NotSupportedException())}LittleEndian(
        finalSpan.Slice({offset} + i * {Utilities.GetDotNetTypeSize(elementType)}, {Utilities.GetDotNetTypeSize(elementType)}),
        {variableName}[i]
    );
}}";
	}

	private static string GenerateEnumSerialization(string variableName, GeneratedMavlinkMessageFieldEnumType enumType, int offset)
	{
		return enumType.ConvertedType switch
		{
			"byte" or "sbyte" => $"finalSpan[{offset}] = (byte){variableName};",
			"ulong" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName});",
			"ushort" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName});",
			"uint" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});",
			"int" => $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});",
			_ => $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.ToInt32(BitConverter.GetBytes({variableName}), 0));"
		};
	}

	private static string GenerateArrayEnumSerialization(string variableName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, int offset, bool isRequired)
	{
		var elementType = arrayEnumType.ConvertedType;
		var arrayLength = arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);
		return $@"
for (int i = 0; i < {arrayEnumType.ArrayLength}; i++)
{{
    System.Buffers.Binary.BinaryPrimitives.Write{(arrayEnumType.ConvertedType == "ushort" ? "UInt16" :
												  arrayEnumType.ConvertedType == "uint" ? "UInt32" :
												  arrayEnumType.ConvertedType == "byte" ? "Byte" :
												  arrayEnumType.ConvertedType == "short" ? "Int16" : throw new NotSupportedException())}LittleEndian(
        finalSpan.Slice({offset} + i * {Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType)}, {Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType)}),
        ({arrayEnumType.ConvertedType}){variableName}[i]
    );
}}";
	}

	private static string GenerateSimpleTypeSerialization(string variableName, string typeName, int offset, bool isRequired)
	{
		return typeName switch
		{
			"byte" => $"finalSpan[{offset}] = {variableName};",
			"sbyte" => $"finalSpan[{offset}] = (byte){variableName};",
			"float" => $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}));",
			"ulong" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice({offset}, 8), {variableName});",
			"ushort" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice({offset}, 2), {variableName});",
			"uint" => $"System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});",
			"int" => $"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), {variableName});",
			_ => $"BitConverter.GetBytes({variableName}).CopyTo(finalSpan.Slice({offset}, {Utilities.GetDotNetTypeSize(typeName)}));"
		};
	}
}
