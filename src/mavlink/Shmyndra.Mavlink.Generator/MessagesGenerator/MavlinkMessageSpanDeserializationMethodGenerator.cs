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
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace);
		}

		foreach (var field in arrayFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace);
		}

		AppendAssignments(methodBody, messageName, fields, @namespace);
		return WrapMethod("DeserializeWithoutExtensions", messageName, methodBody.ToString());
	}

	internal override MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int finalSize = fields.CalculateFinalSize();
		AppendMethodPrologue(methodBody, messageName, finalSize);
		int offset = 0;

		var (requiredFields, arrayFields) = fields.GetSortedFields();

		foreach (var field in requiredFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace);
		}

		foreach (var field in arrayFields)
		{
			AppendFieldDeserialization(methodBody, field, ref offset, @namespace);
		}

		HandleOptionalFields(methodBody, fields, ref offset, @namespace);

		AppendAssignments(methodBody, messageName, fields, @namespace);
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

	private void AppendAssignments(StringBuilder sb, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields, string currentNamespace)
	{
		var assignments = string.Join(",\n", fields.Select(field =>
		{
			var varName = GetVariableName(field.GeneratedName);

			if (field.Type is GeneratedMavlinkMessageFieldEnumType)
			{
				var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
				string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
				return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}Enum";
			}

			return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
		}));
		sb.AppendLine($@"
return new {messageName}
{{
    {assignments}
}};");
	}

	private void HandleOptionalFields(StringBuilder methodBody, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string @namespace)
	{
		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			if (ShouldDeserializeField(field))
			{
				AppendFieldDeserialization(methodBody, field, ref offset, @namespace);
			}
		}
	}

	private void AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string varName = GetVariableName(field.GeneratedName);

		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			AppendArrayFieldDeserialization(sb, arrayType, ref offset, varName);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType)
		{
			AppendEnumFieldDeserialization(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
		{
			AppendArrayEnumFieldDeserialization(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleType)
		{
			AppendSimpleFieldDeserialization(sb, simpleType, ref offset, varName);
		}
	}

	private bool ShouldDeserializeField(GeneratedMavlinkMessageField field)
	{
		return field != null && field.GetFieldSize() > 0;
	}

	private void AppendSimpleFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName)
	{
		int size = Utilities.GetDotNetTypeSize(simpleType.ConvertedType);
		string typeName = simpleType.ConvertedType;
		AppendPrimitiveFieldDeserialization(sb, varName, size, typeName, ref offset);
	}

	private void AppendPrimitiveFieldDeserialization(StringBuilder sb, string varName, int size, string typeName, ref int offset)
	{
		if (typeName == "byte")
		{
			sb.AppendLine($"var {varName} = span[{offset}];");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"var {varName} = (sbyte)span[{offset}];");
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(typeName);
			sb.AppendLine($"var {varName} = BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}));");
		}
		offset += size;
	}

	private void AppendEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
		string valueExpression;

		if (enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte")
		{
			valueExpression = $"span[{offset}]";
		}
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(enumType.ConvertedType);
			valueExpression = $"BinaryPrimitives.{bpMethod}(span.Slice({offset}, {size}))";
		}

		sb.AppendLine($@"
var {varName}Value = {valueExpression};
if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out var {varName}Enum))
{{
    throw new InvalidDataException($""Invalid enum value {{ {varName}Value }} for {enumTypeName}"");
}}");

		offset += size;
	}

	private void AppendArrayFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int arrayByteLength = arrayType.ArrayLength * elementSize;
		string loopCode = GenerateArrayDeserializationLoopSimple(arrayType.ConvertedType, arrayType.ArrayLength, offset, varName);
		sb.AppendLine(loopCode);
		sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	private void AppendArrayEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
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
			sb.AppendLine(GenerateArrayDeserializationLoopEnumWithValidation(arrayEnumType.ConvertedType, arrayEnumType.ArrayLength, offset, varName, enumTypeName));
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
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

		sb.AppendLine($@"
        if (!Enum.TryParse<{enumTypeName}>(value.ToString(), out var {varName}Enum))
        {{
            throw new InvalidDataException($""Invalid enum value {{value}} for {enumTypeName}"");
        }}
        temp{varName}[i_{varName}] = {varName}Enum;");
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
