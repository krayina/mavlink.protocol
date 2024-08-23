using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

internal class MavlinkMessageSerializationGenerator
{
	/// <summary>
	/// Generates the <c>Serialize</c> method for serializing instances of the generated message type into a byte array.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">The array of fields in the Mavlink message, each represented as a <see cref="GeneratedMavlinkMessageField"/>.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the <c>Serialize</c> method.</returns>
	public static MethodDeclarationSyntax CreateSerializeMethod(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		var minSize = fields.CalculateMinSize();

		methodBody.AppendLine($@"
var buffer = new byte[{minSize}];
");

		int currentOffset = 0;

		// Divide fields into required, non-required, and array groups
		var requiredFields = fields.Where(f => f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).ToList();
		var nonRequiredFields = fields.Where(f => !f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType)).ToList();
		var arrayFields = fields.Where(f => f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType).ToList();

		// Sort required fields by type size (largest to smallest), excluding array types
		requiredFields.Sort((field1, field2) =>
		{
			var size1 = Utilities.GetDotNetTypeSize(((GeneratedMavlinkMessageFieldType)field1.Type).ConvertedType);
			var size2 = Utilities.GetDotNetTypeSize(((GeneratedMavlinkMessageFieldType)field2.Type).ConvertedType);
			return size2.CompareTo(size1); // Sort descending
		});

		// Combine sorted required fields, array fields (in original order), and non-required fields (in original order)
		var sortedFields = requiredFields.Concat(arrayFields).Concat(nonRequiredFields).ToList();

		foreach (var field in sortedFields)
		{
			var fieldType = (GeneratedMavlinkMessageFieldType)field.Type;
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			var variableName = $"instance.{fieldPropertyName}";

			if (fieldType is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArraySerialization(variableName, arrayType, currentOffset, field.IsRequired));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumSerialization(variableName, enumType, currentOffset));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumSerialization(variableName, arrayEnumType, currentOffset, field.IsRequired));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeSerialization(variableName, fieldType.ConvertedType, currentOffset, field.IsRequired));
			}

			currentOffset += field.GetFieldSize();
		}

		// Handle extensions
		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			var variableName = $"instance.{fieldPropertyName}";
			var fieldSize = field.GetFieldSize();

			methodBody.AppendLine($@"
if ({variableName}.HasValue)
{{
    Array.Resize(ref buffer, buffer.Length + {fieldSize});
    BitConverter.GetBytes({variableName}.Value).CopyTo(buffer, buffer.Length - {fieldSize});
}}");
		}

		methodBody.AppendLine("return buffer;");

		var methodString = $@"
public static byte[] Serialize({messageName} instance)
{{
    {methodBody}
}}";

		var classWrapper = $@"
public class TemporaryClass
{{
    {methodString}
}}";

		var syntaxTree = CSharpSyntaxTree.ParseText(classWrapper);
		var root = syntaxTree.GetRoot();
		var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Serialize");
		return method;
	}

	private static string GenerateArraySerialization(string variableName, GeneratedMavlinkMessageFieldArrayType arrayType, int offset, bool isRequired)
	{
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);

		if (isRequired)
		{
			return $@"
Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
		}
		else
		{
			return $@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    Array.Resize(ref buffer, {offset} + {arrayLength});
    Buffer.BlockCopy({variableName}.Value.ToArray(), 0, buffer, {offset}, {arrayLength});
}}";
		}
	}

	private static string GenerateEnumSerialization(string variableName, GeneratedMavlinkMessageFieldEnumType enumType, int offset)
	{
		var size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);

		return size == 1
			? $@"
buffer[{offset}] = (byte){variableName};"
			: $@"
BitConverter.GetBytes(({enumType.ConvertedType}){variableName}).CopyTo(buffer, {offset});";
	}

	private static string GenerateArrayEnumSerialization(string variableName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, int offset, bool isRequired)
	{
		var elementType = arrayEnumType.ConvertedType;
		var arrayLength = arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);

		if (isRequired)
		{
			return $@"
Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});";
		}
		else
		{
			return $@"
if ({variableName}.HasValue && !{variableName}.Value.IsDefaultOrEmpty)
{{
    Array.Resize(ref buffer, {offset} + {arrayLength});
    Buffer.BlockCopy({variableName}.Value.ToArray(), 0, buffer, {offset}, {arrayLength});
}}";
		}
	}

	private static string GenerateSimpleTypeSerialization(string variableName, string typeName, int offset, bool isRequired)
	{
		if (isRequired)
		{
			return typeName switch
			{
				"byte" => $@"
buffer[{offset}] = {variableName};",
				"sbyte" => $@"
buffer[{offset}] = (byte){variableName};",
				_ => $@"
BitConverter.GetBytes({variableName}).CopyTo(buffer, {offset});"
			};
		}
		else
		{
			return typeName switch
			{
				"byte?" => $@"
if ({variableName}.HasValue)
{{
    Array.Resize(ref buffer, {offset} + 1);
    buffer[{offset}] = {variableName}.Value;
}}",
				"sbyte?" => $@"
if ({variableName}.HasValue)
{{
    Array.Resize(ref buffer, {offset} + 1);
    buffer[{offset}] = (byte){variableName}.Value;
}}",
				_ => $@"
if ({variableName}.HasValue)
{{
    var valueBytes = BitConverter.GetBytes({variableName}.Value);
    Array.Resize(ref buffer, {offset} + valueBytes.Length);
    valueBytes.CopyTo(buffer, {offset});
}}"
			};
		}
	}

	private static string EscapeReservedKeyword(string name)
	{
		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
	}
}
