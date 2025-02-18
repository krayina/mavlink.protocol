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
if ({DeserializeParameterName}.Length >= {offset} + {fieldSize})
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
if ({DeserializeParameterName}.Length == 0)
{{
    return new {messageName}();
}}

byte[] local = {DeserializeParameterName}.Length < {minSize} ? new byte[{minSize}] : {DeserializeParameterName};
if ({DeserializeParameterName}.Length < {minSize})
{{
    {DeserializeParameterName}.CopyTo(local, 0);
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

		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			AppendArrayFieldDeserialization(sb, arrayType, ref offset, varName, isExtension);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType)
		{
			AppendEnumFieldDeserialization(sb, field, ref offset, varName, isExtension, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
		{
			AppendArrayEnumFieldDeserialization(sb, field, ref offset, varName, isExtension, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleType)
		{
			AppendSimpleFieldDeserialization(sb, simpleType, ref offset, varName, isExtension);
		}
	}

	private void AppendSimpleFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName, bool isExtension)
	{
		int size = Utilities.GetDotNetTypeSize(simpleType.ConvertedType);
		string typeName = simpleType.ConvertedType;
		if (typeName == "byte")
		{
			if (isExtension)
			{
				sb.AppendLine($@"
byte? {varName} = null;
if ({DeserializeParameterName}.Length >= {offset} + 1)
{{
    {varName} = span[{offset}];
}}");
			}
			else
			{
				sb.AppendLine($"var {varName} = span[{offset}];");
			}

			offset += 1;
		}
		else if (typeName == "sbyte")
		{
			if (isExtension)
			{
				sb.AppendLine($@"
sbyte? {varName} = null;
if ({DeserializeParameterName}.Length >= {offset} + 1)
{{
    {varName} = (sbyte)span[{offset}];
}}");
			}
			else
			{
				sb.AppendLine($"var {varName} = (sbyte)span[{offset}];");
			}

			offset += 1;
		}
		else if (typeName == "char")
		{
			if (isExtension)
			{
				sb.AppendLine($@"
ushort? temp{varName} = null;
if ({DeserializeParameterName}.Length >= {offset} + 2)
{{
    temp{varName} = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice({offset}, 2));
}}
char? {varName} = temp{varName}.HasValue ? (char)temp{varName}.Value : null;");
			}
			else
			{
				sb.AppendLine($@"var {varName} = (char)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice({offset}, 2));");
			}

			offset += 2;
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(typeName);
			if (isExtension)
			{
				sb.AppendLine($@"
{typeName}? {varName} = null;
if ({DeserializeParameterName}.Length >= {offset} + {size})
{{
    {varName} = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
}}");
			}
			else
			{
				sb.AppendLine($"var {varName} = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));");
			}

			offset += size;
		}
	}

	private void AppendEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, bool isExtension, string currentNamespace)
	{
		var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
			string bpMethod = GetBinaryPrimitivesMethod(enumType.ConvertedType);
			if (isExtension)
			{
				sb.AppendLine($@"
{enumType.ConvertedType}? {varName}Value = null;
if ({DeserializeParameterName}.Length >= {offset} + {size})
{{
    {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
}}
if (!{varName}Value.HasValue)
{{
    throw new InvalidDataException(""Missing enum value for {enumTypeName}"");
}}
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value.Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value.Value;");
			}
			else
			{
				sb.AppendLine($@"
var {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value;");
			}
			offset += size;
		}
		else
		{
			if (enumType.ConvertedType == "byte")
			{
				if (isExtension)
				{
					sb.AppendLine($@"
byte? {varName}Value = null;
if ({DeserializeParameterName}.Length >= {offset} + 1)
{{
    {varName}Value = span[{offset}];
}}
if (!{varName}Value.HasValue)
{{
    throw new InvalidDataException(""Missing enum value for {enumTypeName}"");
}}
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value.Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value.Value;");
				}
				else
				{
					sb.AppendLine($@"
var {varName}Value = span[{offset}];
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value;");
				}
				offset += 1;
			}
			else if (enumType.ConvertedType == "sbyte")
			{
				if (isExtension)
				{
					sb.AppendLine($@"
sbyte? {varName}Value = null;
if ({DeserializeParameterName}.Length >= {offset} + 1)
{{
    {varName}Value = (sbyte)span[{offset}];
}}
if (!{varName}Value.HasValue)
{{
    throw new InvalidDataException(""Missing enum value for {enumTypeName}"");
}}
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value.Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value.Value;");
				}
				else
				{
					sb.AppendLine($@"
var {varName}Value = (sbyte)span[{offset}];
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value;");
				}
				offset += 1;
			}
			else
			{
				int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
				string bpMethod = GetBinaryPrimitivesMethod(enumType.ConvertedType);
				if (isExtension)
				{
					sb.AppendLine($@"
{enumType.ConvertedType}? {varName}Value = null;
if ({DeserializeParameterName}.Length >= {offset} + {size})
{{
    {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
}}
if (!{varName}Value.HasValue)
{{
    throw new InvalidDataException(""Missing enum value for {enumTypeName}"");
}}
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value.Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value.Value;");
				}
				else
				{
					sb.AppendLine($@"
var {varName}Value = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));
if (!Enum.IsDefined(typeof({enumTypeName}), {varName}Value))
{{
    throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
}}
var {varName} = ({enumTypeName}){varName}Value;");
				}
				offset += size;
			}
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
			sb.AppendLine($@"if ({DeserializeParameterName}.Length >= {offset} + {arrayByteLength})
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

	private void AppendArrayEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, bool isExtension, string currentNamespace)
	{
		var arrayEnumType = (GeneratedMavlinkMessageFieldArrayEnumType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int arrayByteLength = arrayEnumType.ArrayLength * elementSize;
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			sb.AppendLine($"ulong combined = 0;");
			sb.AppendLine($"for (int idx_{varName} = 0; idx_{varName} < {arrayEnumType.ArrayLength}; idx_{varName}++)");
			sb.AppendLine("{");
			sb.AppendLine($"    int elementOffset = {offset} + idx_{varName} * {elementSize};");
			if (arrayEnumType.ConvertedType == "byte")
				sb.AppendLine($"    combined |= ((ulong)span[elementOffset]) << (idx_{varName} * 8);");
			else if (arrayEnumType.ConvertedType == "sbyte")
				sb.AppendLine($"    combined |= ((ulong)(byte)span[elementOffset]) << (idx_{varName} * 8);");
			else if (arrayEnumType.ConvertedType == "char")
				sb.AppendLine($"    combined |= ((ulong)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(elementOffset, 2))) << (idx_{varName} * 16);");
			else
			{
				string bpMethod = GetBinaryPrimitivesMethod(arrayEnumType.ConvertedType);
				int shift = elementSize * 8;
				sb.AppendLine($"    combined |= ((ulong)BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {elementSize}))) << (idx_{varName} * {shift});");
			}
			sb.AppendLine("}");
			sb.AppendLine($"var temp{varName} = new List<{enumTypeName}>();");
			sb.AppendLine($"for (int bit_{varName} = 0; bit_{varName} < {arrayEnumType.ArrayLength * elementSize * 8}; bit_{varName}++)");
			sb.AppendLine("{");
			sb.AppendLine($"    if ((combined & (1UL << bit_{varName})) != 0)");
			sb.AppendLine("    {");
			sb.AppendLine($"        temp{varName}.Add(({enumTypeName})(1UL << bit_{varName}));");
			sb.AppendLine("    }");
			sb.AppendLine("}");
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
			offset += arrayByteLength;
		}
		else
		{
			if (isExtension)
			{
				sb.AppendLine($@"ImmutableArray<{enumTypeName}>? {varName} = null;");
				sb.AppendLine($@"if ({DeserializeParameterName}.Length >= {offset} + {arrayByteLength})
{{
{GenerateArrayDeserializationLoopEnumWithValidation(arrayEnumType.ConvertedType, arrayEnumType.ArrayLength, offset, varName, enumTypeName)}
    {varName} = ImmutableArray.CreateRange(temp{varName});
}}");
			}
			else
			{
				sb.AppendLine(GenerateArrayDeserializationLoopEnumWithValidation(arrayEnumType.ConvertedType, arrayEnumType.ArrayLength, offset, varName, enumTypeName));
				sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
			}
			offset += arrayByteLength;
		}
	}

	private string GenerateArrayDeserializationLoopSimple(string convertedType, int arrayLength, int baseOffset, string varName)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i_{varName} = 0; i_{varName} < {arrayLength}; i_{varName}++)");
		sb.AppendLine("    {");
		sb.AppendLine($"        int elementOffset = {baseOffset} + i_{varName} * {Utilities.GetDotNetTypeSize(convertedType)};");
		if (convertedType == "byte")
		{
			sb.AppendLine($"        temp{varName}[i_{varName}] = span[elementOffset];");
		}
		else if (convertedType == "sbyte")
		{
			sb.AppendLine($"        temp{varName}[i_{varName}] = (sbyte)span[elementOffset];");
		}
		else if (convertedType == "char")
		{
			sb.AppendLine($"        temp{varName}[i_{varName}] = (char)BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(elementOffset, 2));");
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(convertedType);
			sb.AppendLine($"        temp{varName}[i_{varName}] = BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {Utilities.GetDotNetTypeSize(convertedType)}));");
		}
		sb.AppendLine("    }");
		return sb.ToString();
	}

	private string GenerateArrayDeserializationLoopEnumWithValidation(string convertedType, int arrayLength, int baseOffset, string varName, string enumTypeName)
	{
		var sb = new StringBuilder();
		int size = Utilities.GetDotNetTypeSize(convertedType);
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i_{varName} = 0; i_{varName} < {arrayLength}; i_{varName}++)");
		sb.AppendLine("    {");
		sb.AppendLine($"        int elementOffset = {baseOffset} + i_{varName} * {size};");
		if (convertedType == "byte")
		{
			sb.AppendLine($"        var value = span[elementOffset];");
		}
		else if (convertedType == "sbyte")
		{
			sb.AppendLine($"        var value = (sbyte)span[elementOffset];");
		}
		else if (convertedType == "char")
		{
			sb.AppendLine($"        var value = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(elementOffset, 2));");
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(convertedType);
			sb.AppendLine($"        var value = BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {size}));");
		}
		sb.AppendLine($@"        if (!Enum.IsDefined(typeof({enumTypeName}), value))
        {{
            throw new InvalidDataException(""Invalid enum value for {enumTypeName}"");
        }}
        temp{varName}[i_{varName}] = ({enumTypeName})value;");
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
		var lower = char.ToLowerInvariant(generatedName[0]) + generatedName.Substring(1);
		if (lower == DeserializeParameterName)
		{
			return "_" + lower;
		}
		return Utilities.EscapeReservedKeyword(lower);
	}
}
