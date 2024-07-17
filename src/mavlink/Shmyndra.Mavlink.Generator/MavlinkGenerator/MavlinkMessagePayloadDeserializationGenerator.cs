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
		IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var methodBody = new StringBuilder();

		foreach (var field in fields)
		{
			var fieldType = field.Type;
			var fieldName = field.Name;
			var variableName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);

			if (fieldType is FieldArrayType arrayType)
			{
				var elementType = ExtractElementType(arrayType.TypeName);
				methodBody.AppendLine(GenerateArrayDeserialization(variableName, fieldName, arrayType, elementType));
			}
			else if (enumTypes.ContainsKey(fieldType.TypeName))
			{
				methodBody.AppendLine(GenerateEnumDeserialization(variableName, fieldType.TypeName, enumTypes));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeDeserialization(variableName, fieldType.TypeName));
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

	private static string GenerateArrayDeserialization(string variableName, string originalName, FieldArrayType arrayType, string elementType)
	{
		var tempArrayName = $"temp{originalName}Array";
		return $@"
            var {tempArrayName} = new {elementType}[{arrayType.Length}];
            Buffer.BlockCopy(payload, 0, {tempArrayName}, 0, {arrayType.Length} * sizeof({elementType}));
            var {variableName} = {tempArrayName}.ToImmutableArray();";
	}

	private static string GenerateEnumDeserialization(string variableName, string typeName, IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var enumType = enumTypes[typeName];
		return $@"
            var {variableName}Value = BitConverter.ToInt32(payload, 0); // Assuming 4-byte enum
            var {variableName} = ({enumType.Namespace}.{enumType.TypeName}){variableName}Value;";
	}

	private static string GenerateSimpleTypeDeserialization(string variableName, string typeName)
	{
		return typeName switch
		{
			"sbyte" => $@"var {variableName} = (sbyte)payload[0];",
			"byte" => $@"var {variableName} = payload[0];",
			_ => $@"var {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, 0);"
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
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}
}
