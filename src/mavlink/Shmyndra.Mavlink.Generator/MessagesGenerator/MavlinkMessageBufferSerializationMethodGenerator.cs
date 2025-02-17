using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates Mavlink message serialization methods using the traditional buffer approach (BitConverter and Buffer.BlockCopy).
/// </summary>
public class MavlinkMessageBufferSerializationMethodGenerator : MavlinkMessageSerializationMethodGeneratorBase
{
	/// <summary>
	/// Creates a <see cref="GeneratedMavlinkMessageSerializeMethod"/> using the buffer serialization approach.
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

		var minSize = fields.CalculateMinSize();
		methodBody.AppendLine($"var buffer = new byte[{minSize}];");

		int currentOffset = 0;
		var (requiredFields, arrayFields) = GetSortedFields(fields);
		var sortedFields = requiredFields.Concat(arrayFields).ToList();

		// Serialize each required field into the buffer.
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

	internal override MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int baseSize = fields.CalculateMinSize();
		var extensionFields = fields.Where(f => !f.IsRequired).ToList();
		int extensionTotalSize = extensionFields.Sum(f => f.GetFieldSize());
		int totalSize = baseSize + extensionTotalSize;
		methodBody.AppendLine($"var buffer = new byte[{totalSize}];");

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

		int currentLiteralOffset = baseSize;
		foreach (var field in extensionFields)
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			int fieldSize = field.GetFieldSize();
			if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				string convertedType = enumType.ConvertedType;
				methodBody.AppendLine($@"BitConverter.GetBytes({fieldPropertyName}.HasValue ? ({convertedType}){fieldPropertyName}.Value : ({convertedType})0)
    .CopyTo(buffer, {currentLiteralOffset});");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayType)
			{
				methodBody.AppendLine($@"
if ({fieldPropertyName}.HasValue && !{fieldPropertyName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({fieldPropertyName}.Value.ToArray(), 0, buffer, {currentLiteralOffset}, {fieldSize});
}}
else
{{
    for (int i = {currentLiteralOffset}; i < {currentLiteralOffset} + {fieldSize}; i++)
        buffer[i] = 0;
}}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
			{
				methodBody.AppendLine($@"
if ({fieldPropertyName}.HasValue && !{fieldPropertyName}.Value.IsDefaultOrEmpty)
{{
    Buffer.BlockCopy({fieldPropertyName}.Value.ToArray(), 0, buffer, {currentLiteralOffset}, {fieldSize});
}}
else
{{
    for (int i = {currentLiteralOffset}; i < {currentLiteralOffset} + {fieldSize}; i++)
        buffer[i] = 0;
}}");
			}
			else if (field.Type is GeneratedMavlinkMessageFieldType simpleField)
			{
				string convertedType = simpleField.ConvertedType;
				methodBody.AppendLine(GenerateSimpleTypeSerializationWithTernaryOperator(fieldPropertyName, convertedType, currentLiteralOffset, true));
			}
			currentLiteralOffset += fieldSize;
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
		return size == 1
			? $@"buffer[{offset}] = ({enumType.ConvertedType}){variableName};"
			: $@"BitConverter.GetBytes(({enumType.ConvertedType}){variableName}).CopyTo(buffer, {offset});";
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
			"byte" => $@"buffer[{offset}] = {variableName};",
			"sbyte" => $@"buffer[{offset}] = (byte){variableName};",
			_ => $@"BitConverter.GetBytes({variableName}).CopyTo(buffer, {offset});"
		};
	}

	private string GenerateSimpleTypeSerializationWithTernaryOperator(string variableName, string typeName, int literalOffset, bool isRequired)
	{
		return typeName switch
		{
			"byte" => $@"buffer[{literalOffset}] = {variableName}.HasValue ? {variableName}.Value : (byte)0;",
			"sbyte" => $@"buffer[{literalOffset}] = {variableName}.HasValue ? (byte){variableName}.Value : (byte)0;",
			_ => $@"BitConverter.GetBytes({variableName}.HasValue ? {variableName}.Value : 0)
                   .CopyTo(buffer, {literalOffset});"
		};
	}
}
