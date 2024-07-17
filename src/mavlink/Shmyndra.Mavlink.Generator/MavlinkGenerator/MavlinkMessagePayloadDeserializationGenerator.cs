using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkMessagePayloadDeserializationGenerator
{
	public static MethodDeclarationSyntax GenerateCreateInstanceMethod(
		string messageTypeName,
		ImmutableList<(FieldType Type, string Name, string XmlName, string? Description)> fields,
		IImmutableDictionary<string, (string Namespace, string TypeName, string BaseType)> enumTypes)
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
			else if (enumTypes.ContainsKey(fieldType.TypeName))
			{
				var enumTypeInfo = enumTypes[fieldType.TypeName];
				methodBody.AppendLine(GenerateEnumDeserialization(variableName, enumTypeInfo, ref offset));
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

		methodBody.AppendLine($"return new {messageTypeName} {{ {propertiesAssignment} }};");

		var methodString = $@"
public static {messageTypeName} CreateInstance(byte[] payload)
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

	private static string GenerateEnumDeserialization(string variableName, (string Namespace, string TypeName, string BaseType) enumTypeInfo, ref int offset)
	{
		var bitConverterMethod = GetBitConverterMethodForEnumBaseType(enumTypeInfo.BaseType);
		var size = GetTypeSize(enumTypeInfo.BaseType);
		var result = $@"
var {variableName}Value = {bitConverterMethod}(payload, {offset});
var {variableName} = ({enumTypeInfo.Namespace}.{enumTypeInfo.TypeName}){variableName}Value;";
		offset += size;
		return result;
	}

	private static string GenerateSimpleTypeDeserialization(string variableName, string typeName, ref int offset)
	{
		var size = GetTypeSize(typeName);
		var result = typeName switch
		{
			"sbyte" => $@"var {variableName} = (sbyte)payload[{offset}];",
			"byte" => $@"var {variableName} = payload[{offset}];",
			"char" => $@"var {variableName} = BitConverter.ToChar(payload, {offset});",
			_ => $@"var {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, {offset});"
		};
		offset += size;
		return result;
	}

	private static string GetBitConverterMethodForEnumBaseType(string baseType)
	{
		return baseType switch
		{
			"byte" => "(byte)payload",
			"sbyte" => "(sbyte)payload",
			"ushort" => "BitConverter.ToUInt16",
			"short" => "BitConverter.ToInt16",
			"uint" => "BitConverter.ToUInt32",
			"int" => "BitConverter.ToInt32",
			"ulong" => "BitConverter.ToUInt64",
			"long" => "BitConverter.ToInt64",
			"char" => "BitConverter.ToChar",
			_ => throw new NotSupportedException($"Unsupported enum base type: {baseType}")
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
			"sbyte" => "ToSByte", // Додавання підтримки для sbyte
			"byte" => "ToByte", // Додавання підтримки для byte
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
