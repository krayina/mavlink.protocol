using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

internal class MavlinkMessagePayloadDeserializationGenerator
{
	/// <summary>
	/// Generates the <c>CreateInstance</c> method for deserializing Mavlink message payloads into instances of the generated message type.
	/// </summary>
	/// <param name="currentNamespace">The namespace of the generated message type.</param>
	/// <param name="fields">The list of fields in the Mavlink message, each represented as a <see cref="GeneratedMavlinkMessageField"/>.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the <c>CreateInstance</c> method.</returns>
	/// <remarks>
	/// The <c>CreateInstance</c> method is essential for converting raw byte payloads from Mavlink messages into strongly-typed objects, enabling easier manipulation and access to message data in .NET applications.
	/// </remarks>
	/// <exception cref="InvalidCastException">Thrown if any field in <paramref name="fields"/> is not of type <see cref="GeneratedMavlinkMessageFieldType"/> or its derived types.</exception>
	public static MethodDeclarationSyntax GenerateCreateInstanceMethod(
		string currentNamespace,
		ImmutableList<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		var offset = 0;

		foreach (var field in fields)
		{
			var fieldType = (GeneratedMavlinkMessageFieldType)field.Type;
			var fieldPropertyName = field.GeneratedName;
			var variableName = char.ToLowerInvariant(fieldPropertyName[0]) + fieldPropertyName.Substring(1);

			if (fieldType is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArrayDeserialization(variableName, fieldPropertyName, arrayType, ref offset));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumDeserialization(variableName, enumType, ref offset, currentNamespace));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumDeserialization(variableName, fieldPropertyName, arrayEnumType, ref offset, currentNamespace));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeDeserialization(variableName, fieldType.ConvertedType, ref offset));
			}
		}

		var propertiesAssignment = string.Join(", ", fields.Select(field =>
		{
			var variableName = char.ToLowerInvariant(field.GeneratedName[0]) + field.GeneratedName.Substring(1);
			return $"{field.GeneratedName} = {variableName}";
		}));

		methodBody.AppendLine($"return new {fields.First().Type.GetType().Name} {{ {propertiesAssignment} }};");

		var methodString = $@"
public static {fields.First().Type.GetType().Name} CreateInstance(byte[] payload)
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
		var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "CreateInstance");
		return method;
	}

	private static string GenerateArrayDeserialization(string variableName, string propertyName, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset)
	{
		var tempArrayName = $"temp{propertyName}Array";
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * GetTypeSize(elementType);
		var result = $@"
var {tempArrayName} = new {elementType}[{arrayType.ArrayLength}];
Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayLength});
var {variableName} = {tempArrayName}.ToImmutableArray();";
		offset += arrayLength;
		return result;
	}

	private static string GenerateEnumDeserialization(string variableName, GeneratedMavlinkMessageFieldEnumType fieldEnumType, ref int offset, string currentNamespace)
	{
		var size = GetTypeSize(fieldEnumType.ConvertedType);
		var (enumNamespace, enumTypeName) = (fieldEnumType.GeneratedEnum.Namespace, fieldEnumType.GeneratedEnum.Name);
		var fullEnumTypeName = enumNamespace == currentNamespace ? enumTypeName : $"{enumNamespace}.{enumTypeName}";

		string deserializationCode;
		if (size == 1)
		{
			deserializationCode = $@"
var {variableName}Value = (byte)payload[{offset}];
var {variableName} = ({fullEnumTypeName}){variableName}Value;";
		}
		else
		{
			deserializationCode = $@"
var {variableName}Value = BitConverter.{GetBitConverterMethodForSize(size)}(payload, {offset});
var {variableName} = ({fullEnumTypeName}){variableName}Value;";
		}

		offset += size;
		return deserializationCode;
	}

	private static string GenerateArrayEnumDeserialization(string variableName, string propertyName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string currentNamespace)
	{
		var tempArrayName = $"temp{propertyName}Array";
		var (enumNamespace, enumTypeName) = (arrayEnumType.GeneratedEnum.Namespace, arrayEnumType.GeneratedEnum.Name);
		var fullEnumTypeName = enumNamespace == currentNamespace ? enumTypeName : $"{enumNamespace}.{enumTypeName}";

		var result = $@"
var {tempArrayName} = new {fullEnumTypeName}[{arrayEnumType.ArrayLength}];
Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayEnumType.ArrayLength * GetTypeSize(arrayEnumType.ConvertedType)});
var {variableName} = {tempArrayName}.ToImmutableArray();";
		offset += arrayEnumType.ArrayLength * GetTypeSize(arrayEnumType.ConvertedType);
		return result;
	}

	private static string GenerateSimpleTypeDeserialization(string variableName, string typeName, ref int offset)
	{
		var size = GetTypeSize(typeName);
		string deserializationCode;

		if (typeName == "byte")
		{
			deserializationCode = $@"var {variableName} = payload[{offset}];";
		}
		else if (typeName == "sbyte")
		{
			deserializationCode = $@"var {variableName} = (sbyte)payload[{offset}];";
		}
		else
		{
			deserializationCode = $@"var {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, {offset});";
		}

		offset += size;
		return deserializationCode;
	}

	private static string GetBitConverterMethodForSize(int size)
	{
		return size switch
		{
			1 => "ToByte",
			2 => "ToUInt16",
			4 => "ToUInt32",
			8 => "ToUInt64",
			_ => throw new NotSupportedException($"Unsupported size: {size}")
		};
	}

	private static string GetBitConverterMethod(string typeName)
	{
		return typeName switch
		{
			"int" => "ToInt32",
			"uint" => "ToUInt32",
			"short" => "ToInt16",
			"ushort" => "ToUInt16",
			"long" => "ToInt64",
			"ulong" => "ToUInt64",
			"float" => "ToSingle",
			"double" => "ToDouble",
			"sbyte" => "ToSByte",
			"byte" => "ToByte",
			"char" => "ToChar",
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}

	private static int GetTypeSize(string typeName)
	{
		return typeName switch
		{
			"byte" => 1,
			"sbyte" => 1,
			"ushort" => 2,
			"short" => 2,
			"uint" => 4,
			"int" => 4,
			"ulong" => 8,
			"long" => 8,
			"float" => 4,
			"double" => 8,
			"char" => 2,
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}
}
