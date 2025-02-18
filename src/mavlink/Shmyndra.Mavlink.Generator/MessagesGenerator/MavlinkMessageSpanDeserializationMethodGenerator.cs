using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator.MessagesGenerator;

public class MavlinkMessageSpanDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGeneratorBase
{
	public override GeneratedMavlinkMessageDeserializeMethod CreateDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var deserializeWithoutExtensionsMethod = CreateDeserializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		var deserializeWithExtensionsMethod = fields.Any(x => !x.IsRequired)
			? CreateDeserializeWithExtensionsMethodInternal(@namespace, messageName, fields)
			: null;
		return new GeneratedMavlinkMessageDeserializeMethod(@namespace, messageName, fields, deserializeWithoutExtensionsMethod, deserializeWithExtensionsMethod);
	}

	internal override MethodDeclarationSyntax CreateDeserializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int minSize = fields.CalculateMinSize();
		AppendMethodPrologue(methodBody, messageName, minSize);
		int offset = 0;

		var (requiredFields, arrayFields) = fields.GetSortedFields();

		foreach (var field in requiredFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace, isExtension: false);
		}

		foreach (var field in arrayFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace, isExtension: false);
		}

		AppendAssignments(methodBody, messageName, fields);
		return WrapMethod("DeserializeWithoutExtensions", messageName, methodBody.ToString());
	}

	internal override MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int minSize = fields.CalculateMinSize();
		AppendMethodPrologue(methodBody, messageName, minSize);
		int offset = 0;

		var (requiredFields, arrayFields) = fields.GetSortedFields();

		foreach (var field in requiredFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace, isExtension: false);
		}

		foreach (var field in arrayFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace, isExtension: false);
		}

		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			int fieldSize = field.GetFieldSize();
			methodBody.AppendLine($@"
if (payload.Length >= {offset} + {fieldSize})
{{");
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace, isExtension: true);
			methodBody.AppendLine("}");
		}

		AppendAssignments(methodBody, messageName, fields);
		return WrapMethod("DeserializeWithExtensions", messageName, methodBody.ToString());
	}

	private void AppendMethodPrologue(StringBuilder sb, string messageName, int minSize)
	{
		sb.AppendLine($@"
if (payload.Length == 0)
{{
    return new {messageName}();
}}

Span<byte> local = payload.Length < {minSize} ? stackalloc byte[{minSize}] : payload;
if (payload.Length < {minSize})
{{
    payload.CopyTo(local);
    local.Slice(payload.Length).Clear();
}}
ReadOnlySpan<byte> span = local;");
	}

	private void AppendAssignments(StringBuilder sb, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var assignments = string.Join(",\n", fields.Select(field =>
		{
			var varName = GetVariableName(field.GeneratedName);
			return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
		}));
		sb.AppendLine($@"
return new {messageName}
{{
    {assignments}
}};");
	}

	private void AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, bool isExtension)
	{
		string varName = GetVariableName(field.GeneratedName);
		if (varName == "payload")
			varName = "_" + varName;
		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
			AppendArrayFieldDeserialization(sb, arrayType, ref offset, varName, isExtension);
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
			AppendEnumFieldDeserialization(sb, enumType, ref offset, varName, isExtension, currentNamespace);
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
			AppendArrayEnumFieldDeserialization(sb, arrayEnumType, ref offset, varName, isExtension, currentNamespace);
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleType)
			AppendSimpleFieldDeserialization(sb, simpleType, ref offset, varName, isExtension);
	}

	private void AppendSimpleFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName, bool isExtension)
	{
		int size = Utilities.GetDotNetTypeSize(simpleType.ConvertedType);
		string typeName = simpleType.ConvertedType;
		if (typeName == "byte")
		{
			if (isExtension)
				sb.AppendLine($@"
byte? {varName} = null;
if (payload.Length >= {offset} + 1)
{{
    {varName} = span[{offset}];
}}");
			else
				sb.AppendLine($"var {varName} = span[{offset}];");
			offset += 1;
		}
		else if (typeName == "sbyte")
		{
			if (isExtension)
				sb.AppendLine($@"
sbyte? {varName} = null;
if (payload.Length >= {offset} + 1)
{{
    {varName} = (sbyte)span[{offset}];
}}");
			else
				sb.AppendLine($"var {varName} = (sbyte)span[{offset}];");
			offset += 1;
		}
		else if (typeName == "char")
		{
			if (isExtension)
				sb.AppendLine($@"
ushort? temp{varName} = null;
if (payload.Length >= {offset} + 2)
{{
    temp{varName} = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice({offset}, 2));
}}
char? {varName} = temp{varName}.HasValue ? (char)temp{varName}.Value : null;");
			else
				sb.AppendLine($@"var {varName} = (char)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice({offset}, 2));");
			offset += 2;
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(typeName);
			if (isExtension)
				sb.AppendLine($@"
{typeName}? {varName} = null;
if (payload.Length >= {offset} + {size})
{{
    {varName} = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
}}");
			else
				sb.AppendLine($"var {varName} = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));");
			offset += size;
		}
	}

	private void AppendEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string varName, bool isExtension, string currentNamespace)
	{
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		if (enumType.ConvertedType == "byte")
		{
			if (isExtension)
				sb.AppendLine($@"
byte? {varName}Value = null;
if (payload.Length >= {offset} + 1)
{{
    {varName}Value = span[{offset}];
}}
var {varName} = {varName}Value.HasValue ? ({enumTypeName}?){varName}Value.Value : null;");
			else
				sb.AppendLine($"var {varName} = ({enumTypeName})span[{offset}];");
			offset += 1;
		}
		else if (enumType.ConvertedType == "sbyte")
		{
			if (isExtension)
				sb.AppendLine($@"
sbyte? {varName}Value = null;
if (payload.Length >= {offset} + 1)
{{
    {varName}Value = (sbyte)span[{offset}];
}}
var {varName} = {varName}Value.HasValue ? ({enumTypeName}?){varName}Value.Value : null;");
			else
				sb.AppendLine($"var {varName} = ({enumTypeName})(sbyte)span[{offset}];");
			offset += 1;
		}
		else
		{
			int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
			string bpMethod = GetBinaryPrimitivesMethod(enumType.ConvertedType);
			if (isExtension)
				sb.AppendLine($@"
{enumType.ConvertedType}? {varName}Value = null;
if (payload.Length >= {offset} + {size})
{{
    {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
}}
var {varName} = {varName}Value.HasValue ? ({enumTypeName}?){varName}Value.Value : null;");
			else
				sb.AppendLine($@"
var {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
var {varName} = ({enumTypeName}){varName}Value;");
			offset += size;
		}
	}

	private void AppendArrayFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName, bool isExtension)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int arrayByteLength = arrayType.ArrayLength * elementSize;
		string loopCode = GenerateArrayDeserializationLoopSimple(arrayType.ConvertedType, arrayType.ArrayLength, offset, varName);
		if (isExtension)
		{
			sb.AppendLine($@"ImmutableArray<{arrayType.ConvertedType}>? {varName} = null;");
			sb.AppendLine($@"if (payload.Length >= {offset} + {arrayByteLength})
{{
{loopCode}
    {varName} = ImmutableArray.CreateRange(temp{varName});
}}");
		}
		else
		{
			sb.AppendLine(loopCode);
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		}
		offset += arrayByteLength;
	}

	private void AppendArrayEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string varName, bool isExtension, string currentNamespace)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int arrayByteLength = arrayEnumType.ArrayLength * elementSize;
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;
		string loopCode = GenerateArrayDeserializationLoopEnum(arrayEnumType.ConvertedType, arrayEnumType.ArrayLength, offset, varName, enumTypeName);
		if (isExtension)
		{
			sb.AppendLine($@"ImmutableArray<{enumTypeName}>? {varName} = null;");
			sb.AppendLine($@"if (payload.Length >= {offset} + {arrayByteLength})
{{
{loopCode}
    {varName} = ImmutableArray.CreateRange(temp{varName});
}}");
		}
		else
		{
			sb.AppendLine(loopCode);
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		}
		offset += arrayByteLength;
	}

	private string GenerateArrayDeserializationLoopSimple(string convertedType, int arrayLength, int baseOffset, string varName)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("    {");
		sb.AppendLine($"        int elementOffset = {baseOffset} + i * {Utilities.GetDotNetTypeSize(convertedType)};");
		if (convertedType == "byte")
			sb.AppendLine($"        temp{varName}[i] = span[elementOffset];");
		else if (convertedType == "sbyte")
			sb.AppendLine($"        temp{varName}[i] = (sbyte)span[elementOffset];");
		else if (convertedType == "char")
			sb.AppendLine($"        temp{varName}[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(elementOffset, 2));");
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(convertedType);
			sb.AppendLine($"        temp{varName}[i] = BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {Utilities.GetDotNetTypeSize(convertedType)}));");
		}
		sb.AppendLine("    }");
		return sb.ToString();
	}

	private string GenerateArrayDeserializationLoopEnum(string convertedType, int arrayLength, int baseOffset, string varName, string enumTypeName)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i = 0; i < {arrayLength}; i++)");
		sb.AppendLine("    {");
		sb.AppendLine($"        int elementOffset = {baseOffset} + i * {Utilities.GetDotNetTypeSize(convertedType)};");
		if (convertedType == "byte")
			sb.AppendLine($"        temp{varName}[i] = span[elementOffset];");
		else if (convertedType == "sbyte")
			sb.AppendLine($"        temp{varName}[i] = (sbyte)span[elementOffset];");
		else if (convertedType == "char")
			sb.AppendLine($"        temp{varName}[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(elementOffset, 2));");
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(convertedType);
			sb.AppendLine($"        var value = BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {Utilities.GetDotNetTypeSize(convertedType)}));");
			sb.AppendLine($"        temp{varName}[i] = ({enumTypeName})value;");
		}
		sb.AppendLine("    }");
		return sb.ToString();
	}

	private string GetQualifiedEnumTypeName(GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return enumType.GeneratedEnum.Namespace == currentNamespace
			? enumType.GeneratedEnum.GeneratedName
			: $"{enumType.GeneratedEnum.Namespace}.{enumType.GeneratedEnum.GeneratedName}";
	}

	private string GetBinaryPrimitivesMethod(string typeName)
	{
		return typeName switch
		{
			"int" => "ReadInt32LittleEndian",
			"uint" => "ReadUInt32LittleEndian",
			"short" => "ReadInt16LittleEndian",
			"ushort" => "ReadUInt16LittleEndian",
			"long" => "ReadInt64LittleEndian",
			"ulong" => "ReadUInt64LittleEndian",
			"float" => "ReadSingleLittleEndian",
			"double" => "ReadDoubleLittleEndian",
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}

	private string GetVariableName(string generatedName)
	{
		var name = Utilities.EscapeReservedKeyword(generatedName);
		return char.ToLowerInvariant(name[0]) + name.Substring(1);
	}
}
