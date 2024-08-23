using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

internal class MavlinkMessageDeserializationGenerator
{
	private const string CreateRangeWithNamespace = "System.Collections.Immutable.ImmutableArray.CreateRange";
	private const string DeserializeParameterName = "payload";

	/// <summary>
	/// Generates the <c>Deserialize</c> method for deserializing Mavlink message payloads into instances of the generated message type.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">The array of fields in the Mavlink message, each represented as a <see cref="GeneratedMavlinkMessageField"/>.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the <c>Deserialize</c> method.</returns>
	/// <remarks>
	/// The <c>Deserialize</c> method is essential for converting raw byte payloads from Mavlink messages into strongly-typed objects, enabling easier manipulation and access to message data in .NET applications.
	/// </remarks>
	/// <exception cref="InvalidCastException">Thrown if any field in <paramref name="fields"/> is not of type <see cref="GeneratedMavlinkMessageFieldType"/> or its derived types.</exception>
	public static MethodDeclarationSyntax CreateDeserializeMethod(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();

		methodBody.AppendLine($@"
if (payload.Length == 0)
{{
    return new {messageName}();
}}");

		var minSize = fields.CalculateMinSize();
		methodBody.AppendLine($@"
if (payload.Length < {minSize})
{{
    var paddedPayload = new byte[{minSize}];
    Array.Copy(payload, paddedPayload, payload.Length);
    payload = paddedPayload;
}}");

		var offset = 0;

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
			var variableName = EscapeReservedKeyword(char.ToLowerInvariant(fieldPropertyName[0]) + fieldPropertyName.Substring(1));

			if (variableName == DeserializeParameterName)
			{
				variableName = "_" + variableName;
			}

			if (!field.IsRequired)
			{
				var fieldSize = field.GetFieldSize();

				methodBody.AppendLine($@"
if (payload.Length > {offset} && payload.Length < {offset + fieldSize})
{{
    var paddedPayload = new byte[{offset + fieldSize}];
    Array.Copy(payload, paddedPayload, payload.Length);
    payload = paddedPayload;
}}");
			}

			if (fieldType is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				methodBody.AppendLine(GenerateArrayDeserialization(variableName, fieldPropertyName, arrayType, ref offset, field.IsRequired));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldEnumType enumType)
			{
				methodBody.AppendLine(GenerateEnumDeserialization(variableName, enumType, ref offset, @namespace, field.IsRequired));
			}
			else if (fieldType is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			{
				methodBody.AppendLine(GenerateArrayEnumDeserialization(variableName, fieldPropertyName, arrayEnumType, ref offset, @namespace, field.IsRequired));
			}
			else
			{
				methodBody.AppendLine(GenerateSimpleTypeDeserialization(variableName, fieldType.ConvertedType, ref offset, field.IsRequired));
			}
		}

		var propertiesAssignment = string.Join(", ", sortedFields.Select(field =>
		{
			var variableName = EscapeReservedKeyword(char.ToLowerInvariant(field.GeneratedName[0]) + field.GeneratedName.Substring(1));
			if (variableName == DeserializeParameterName)
			{
				variableName = "_" + variableName;
			}
			return $"{EscapeReservedKeyword(field.GeneratedName)} = {variableName}";
		}));

		methodBody.AppendLine($"return new {messageName} {{ {propertiesAssignment} }};");

		var methodString = $@"
public static {messageName} Deserialize(byte[] payload)
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
		var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Deserialize");
		return method;
	}

	private static string GenerateArrayDeserialization(string variableName, string propertyName, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, bool isRequired)
	{
		var tempArrayName = $"temp{propertyName}Array";
		var elementType = arrayType.ConvertedType;
		var arrayLength = arrayType.ArrayLength * Utilities.GetDotNetTypeSize(elementType);
		var result = new StringBuilder();

		if (isRequired)
		{
			result.AppendLine($@"
var {tempArrayName} = new {elementType}[{arrayType.ArrayLength}];
Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayLength});
var {variableName} = {CreateRangeWithNamespace}({tempArrayName});");
		}
		else
		{
			result.AppendLine($@"
{MavlinkGeneratorConstants.ImmutableArrayNamespace}<{elementType}>? {variableName} = null;
if (payload.Length >= {offset + arrayLength})
{{
    var {tempArrayName} = new {elementType}[{arrayType.ArrayLength}];
    Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayLength});
    {variableName} = {CreateRangeWithNamespace}({tempArrayName});
}}");
		}

		offset += arrayLength;
		return result.ToString();
	}

	private static string GenerateEnumDeserialization(string variableName, GeneratedMavlinkMessageFieldEnumType fieldEnumType, ref int offset, string currentNamespace, bool isRequired)
	{
		var size = Utilities.GetDotNetTypeSize(fieldEnumType.ConvertedType);
		var (enumNamespace, enumTypeName) = (fieldEnumType.GeneratedEnum.Namespace, fieldEnumType.GeneratedEnum.GeneratedName);
		var fullEnumTypeName = enumNamespace == currentNamespace ? enumTypeName : $"{enumNamespace}.{enumTypeName}";

		var result = new StringBuilder();

		if (isRequired)
		{
			if (size == 1)
			{
				result.AppendLine($@"
var {variableName}Value = (byte)payload[{offset}];
var {variableName} = ({fullEnumTypeName}){variableName}Value;");
			}
			else
			{
				result.AppendLine($@"
var {variableName}Value = BitConverter.{GetBitConverterMethodForSize(size)}(payload, {offset});
var {variableName} = ({fullEnumTypeName}){variableName}Value;");
			}
		}
		else
		{
			if (size == 1)
			{
				result.AppendLine($@"
byte? {variableName}Value = null;
if (payload.Length > {offset})
{{
    {variableName}Value = (byte)payload[{offset}];
}}
var {variableName} = {variableName}Value.HasValue ? ({fullEnumTypeName}?){variableName}Value.Value : null;");
			}
			else
			{
				result.AppendLine($@"
{fieldEnumType.ConvertedType}? {variableName}Value = null;
if (payload.Length > {offset})
{{
    {variableName}Value = BitConverter.{GetBitConverterMethodForSize(size)}(payload, {offset});
}}
var {variableName} = {variableName}Value.HasValue ? ({fullEnumTypeName}?){variableName}Value.Value : null;");
			}
		}

		offset += size;
		return result.ToString();
	}

	private static string GenerateArrayEnumDeserialization(string variableName, string propertyName, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string currentNamespace, bool isRequired)
	{
		var tempArrayName = $"temp{propertyName}Array";
		var (enumNamespace, enumTypeName) = (arrayEnumType.GeneratedEnum.Namespace, arrayEnumType.GeneratedEnum.GeneratedName);
		var fullEnumTypeName = enumNamespace == currentNamespace ? enumTypeName : $"{enumNamespace}.{enumTypeName}";

		var result = new StringBuilder();

		if (isRequired)
		{
			result.AppendLine($@"
var {tempArrayName} = new {fullEnumTypeName}[{arrayEnumType.ArrayLength}];
Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType)});
var {variableName} = {CreateRangeWithNamespace}({tempArrayName});");
		}
		else
		{
			result.AppendLine($@"
{MavlinkGeneratorConstants.ImmutableArrayNamespace}<{fullEnumTypeName}>? {variableName} = null;
if (payload.Length >= {offset + arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType)})
{{
    var {tempArrayName} = new {fullEnumTypeName}[{arrayEnumType.ArrayLength}];
    Buffer.BlockCopy(payload, {offset}, {tempArrayName}, 0, {arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType)});
    {variableName} = {CreateRangeWithNamespace}({tempArrayName});
}}");
		}

		offset += arrayEnumType.ArrayLength * Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		return result.ToString();
	}

	private static string GenerateSimpleTypeDeserialization(string variableName, string typeName, ref int offset, bool isRequired)
	{
		var size = Utilities.GetDotNetTypeSize(typeName);
		var result = new StringBuilder();

		if (isRequired)
		{
			result.AppendLine(typeName switch
			{
				"byte" => $@"var {variableName} = payload[{offset}];",
				"sbyte" => $@"var {variableName} = (sbyte)payload[{offset}];",
				_ => $@"var {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, {offset});"
			});
		}
		else
		{
			result.AppendLine(typeName switch
			{
				"byte" => $@"
byte? {variableName} = null;
if (payload.Length > {offset})
{{
    {variableName} = payload[{offset}];
}}",
				"sbyte" => $@"
sbyte? {variableName} = null;
if (payload.Length > {offset})
{{
    {variableName} = (sbyte)payload[{offset}];
}}",
				_ => $@"
{typeName}? {variableName} = null;
if (payload.Length > {offset})
{{
    {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, {offset});
}}"
			});
		}

		offset += size;
		return result.ToString();
	}

	private static string EscapeReservedKeyword(string name)
	{
		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
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
}
