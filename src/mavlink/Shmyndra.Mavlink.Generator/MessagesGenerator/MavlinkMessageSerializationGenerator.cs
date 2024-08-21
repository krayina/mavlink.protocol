using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Text;

namespace Shmyndra.Mavlink.Generator.MessagesGenerator;

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
		var totalSize = CalculateTotalSize(fields);

		methodBody.AppendLine($@"
var buffer = new byte[{totalSize}];
");

		int currentOffset = 0;

		foreach (var field in fields)
		{
			var fieldType = (GeneratedMavlinkMessageFieldType)field.Type;
			var fieldPropertyName = EscapeReservedKeyword(field.GeneratedName);
			var variableName = $"instance.{fieldPropertyName}";

			if (fieldType is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArraySerialization(variableName, arrayType, currentOffset));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumSerialization(variableName, enumType, currentOffset));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumSerialization(variableName, arrayEnumType, currentOffset));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeSerialization(variableName, fieldType.ConvertedType, currentOffset, field.IsRequired));
			}

			currentOffset += field.GetFieldSize();
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

	private static string GenerateArraySerialization(string variableName, GeneratedMavlinkMessageFieldArrayType arrayType, int offset)
	{
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);

		return $@"
if ({variableName} != null)
{{
    Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});
}}";
	}

	private static string GenerateEnumSerialization(string variableName, GeneratedMavlinkMessageFieldEnumType enumType, int offset)
	{
		var size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);

		return size == 1
			? $@"
buffer[{offset}] = (byte){variableName};"
			: $@"
BitConverter.GetBytes((uint){variableName}).CopyTo(buffer, {offset});";
	}

	private static string GenerateArrayEnumSerialization(string variableName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, int offset)
	{
		var elementType = arrayEnumType.ConvertedType;
		var arrayLength = arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);

		return $@"
if ({variableName} != null)
{{
    Buffer.BlockCopy({variableName}.ToArray(), 0, buffer, {offset}, {arrayLength});
}}";
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
    buffer[{offset}] = {variableName}.Value;
}}",
				"sbyte?" => $@"
if ({variableName}.HasValue)
{{
    buffer[{offset}] = (byte){variableName}.Value;
}}",
				_ => $@"
if ({variableName}.HasValue)
{{
    BitConverter.GetBytes({variableName}.Value).CopyTo(buffer, {offset});
}}"
			};
		}
	}

	private static string EscapeReservedKeyword(string name)
	{
		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
	}

	private static int CalculateTotalSize(ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		int totalSize = 0;

		foreach (var field in fields)
		{
			int fieldSize = field.GetFieldSize();
			totalSize += fieldSize;
		}

		return totalSize;
	}
}
