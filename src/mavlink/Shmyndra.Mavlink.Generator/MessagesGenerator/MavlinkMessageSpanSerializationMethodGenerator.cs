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
		var nonRequiredFields = fields.Where(f => !f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).ToList();
		int extensionLength = nonRequiredFields.Sum(f => f.GetFieldSize());
		int finalSize = minSize + extensionLength;
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
		foreach (var field in nonRequiredFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			int fieldSize = field.GetFieldSize();
			if (field.Type is GeneratedMavlinkMessageFieldType simpleField && ((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType == "byte")
			{
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    BitConverter.GetBytes({fieldPropertyName}.Value).CopyTo(finalSpan.Slice({currentOffset}, {fieldSize}));");
				methodBody.AppendLine("}");
				methodBody.AppendLine("else");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    finalSpan.Slice({currentOffset}, {fieldSize}).Fill(0);");
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				string convertedType = enumType.ConvertedType;
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    BitConverter.GetBytes(({convertedType}){fieldPropertyName}.Value).CopyTo(finalSpan.Slice({currentOffset}, {fieldSize}));");
				methodBody.AppendLine("}");
				methodBody.AppendLine("else");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    finalSpan.Slice({currentOffset}, {fieldSize}).Fill(0);");
				methodBody.AppendLine("}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldType simpleField2)
			{
				string convertedType = simpleField2.ConvertedType;
				methodBody.AppendLine($"if ({fieldPropertyName}.HasValue)");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    BitConverter.GetBytes({fieldPropertyName}.Value).CopyTo(finalSpan.Slice({currentOffset}, {fieldSize}));");
				methodBody.AppendLine("}");
				methodBody.AppendLine("else");
				methodBody.AppendLine("{");
				methodBody.AppendLine($"    finalSpan.Slice({currentOffset}, {fieldSize}).Fill(0);");
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
		return $"Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
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
		return $"Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
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
