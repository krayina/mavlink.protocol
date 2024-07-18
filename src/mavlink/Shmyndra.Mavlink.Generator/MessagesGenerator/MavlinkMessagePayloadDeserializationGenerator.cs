using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkMessagePayloadDeserializationGenerator
{
	public static MethodDeclarationSyntax GenerateCreateInstanceMethod(
		(string Namespace, string Name) messageType,
		ImmutableList<(FieldType Type, string Name, string XmlName, string? Description)> fields,
		IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var methodBody = new StringBuilder();
		var offset = 0;

		foreach (var field in fields)
		{
			var fieldType = field.Type;
			var fieldName = field.Name;
			var variableName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);

			if (fieldType is FieldArrayType arrayType)
			{
				var elementType = ExtractElementType(arrayType.TypeName);
				methodBody.AppendLine(GenerateArrayDeserialization(variableName, fieldName, arrayType, elementType, ref offset));
			}
			else if (fieldType is FieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumDeserialization(variableName, enumType, enumTypes, ref offset, messageType.Namespace));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeDeserialization(variableName, fieldType.TypeName, ref offset));
			}
		}

		var propertiesAssignment = string.Join(", ", fields.Select(field =>
		{
			var variableName = char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
			return $"{field.Name} = {variableName}";
		}));

		methodBody.AppendLine($"return new {messageType.Name} {{ {propertiesAssignment} }};");

		var methodString = $@"
public static {messageType.Name} CreateInstance(byte[] payload)
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

	private static string ExtractElementType(string typeName)
	{
		var start = typeName.IndexOf('<') + 1;
		var length = typeName.IndexOf('>') - start;
		return typeName.Substring(start, length);
	}

	private static string GenerateArrayDeserialization(string variableName, string originalName, FieldArrayType arrayType, string elementType, ref int offset)
	{
		var tempArrayName = $"temp{originalName}Array";
		var arrayLength = arrayType.Length * GetTypeSize(elementType);
		var result = $@"
var {tempArrayName} = new {elementType}[{arrayType.Length}];
Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayLength});
var {variableName} = {tempArrayName}.ToImmutableArray();";
		offset += arrayLength;
		return result;
	}

	private static string GenerateEnumDeserialization(string variableName, FieldEnumType fieldEnumType, IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes, ref int offset, string currentNamespace)
	{
		var size = fieldEnumType.Size;
		var (enumNamespace, enumTypeName) = enumTypes[fieldEnumType.TypeName];
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
