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
	/// <summary>
	/// Creates a <see cref="GeneratedMavlinkMessageSerializeMethod"/> using the Span serialization approach.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>
	/// A <see cref="GeneratedMavlinkMessageSerializeMethod"/> containing both the SerializeWithoutExtensions and
	/// SerializeWithExtensions methods for the message.
	/// </returns>
	public override GeneratedMavlinkMessageSerializeMethod CreateSerializeMethod(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var serializeWithoutExtensionsMethod = CreateSerializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		var serializeWithExtensionsMethod = fields.Any(x => !x.IsRequired) ? CreateSerializeWithExtensionsMethodInternal(@namespace, messageName, fields) : null;
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

		// Serialize required fields using Span.
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

		// Base buffer.
		var minSize = fields.CalculateMinSize();
		methodBody.AppendLine($"byte[] baseBuffer = new byte[{minSize}];");
		methodBody.AppendLine("Span<byte> baseSpan = baseBuffer.AsSpan();");

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

		// Process non-required fields (extensions).
		var nonRequiredFields = fields.Where(f => !f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType ||
																	 f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).ToList();
		methodBody.AppendLine("int extensionLength = 0;");
		foreach (var field in nonRequiredFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			var fieldSize = field.GetFieldSize();
			methodBody.AppendLine($@"if ({fieldPropertyName}.HasValue)
    extensionLength += {fieldSize};");
		}
		methodBody.AppendLine($"byte[] buffer = new byte[{minSize} + extensionLength];");
		methodBody.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");
		methodBody.AppendLine("baseBuffer.AsSpan().CopyTo(finalSpan);");
		methodBody.AppendLine("int offset = baseBuffer.Length;");

		foreach (var field in nonRequiredFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			var fieldSize = field.GetFieldSize();
			if (field.Type is GeneratedMavlinkMessageFieldType fieldType &&
				((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType == "byte")
			{
				methodBody.AppendLine($@"
if ({fieldPropertyName}.HasValue)
{{
    finalSpan[offset] = {fieldPropertyName}.Value;
    offset += 1;
}}");
				continue;
			}
			methodBody.AppendLine($@"
if ({fieldPropertyName}.HasValue)
{{
    var valueBytes = BitConverter.GetBytes({fieldPropertyName}.Value);
    valueBytes.CopyTo(finalSpan.Slice(offset, {fieldSize}));
    offset += {fieldSize};
}}");
		}
		methodBody.AppendLine("return buffer;");
		return WrapMethod("SerializeWithExtensions", methodBody.ToString());
	}

	private string GenerateArraySerialization(string variableName, GeneratedMavlinkMessageFieldArrayType arrayType, int offset, bool isRequired)
	{
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);
		return $@"Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
	}

	private string GenerateEnumSerialization(string variableName, GeneratedMavlinkMessageFieldEnumType enumType, int offset)
	{
		var size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
		if (size == 1)
		{
			return $@"finalSpan[{offset}] = (byte){variableName};";
		}
		else
		{
			return $@"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits((float){variableName}));";
		}
	}

	private string GenerateArrayEnumSerialization(string variableName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, int offset, bool isRequired)
	{
		var elementType = arrayEnumType.ConvertedType;
		var arrayLength = arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);
		return $@"Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
	}

	private string GenerateSimpleTypeSerialization(string variableName, string typeName, int offset, bool isRequired)
	{
		return typeName switch
		{
			"byte" => $@"finalSpan[{offset}] = {variableName};",
			"sbyte" => $@"finalSpan[{offset}] = (byte){variableName};",
			"float" => $@"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(finalSpan.Slice({offset}, 4), BitConverter.SingleToInt32Bits({variableName}));",
			_ => $@"BitConverter.GetBytes({variableName}).CopyTo(buffer, {offset});"
		};
	}
}
