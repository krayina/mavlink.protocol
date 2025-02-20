using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator;

internal class MavlinkMessageBufferDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGeneratorBase
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
		methodBody.AppendLine($@"
if ({DeserializeParameterName}.Length == 0)
{{
    return new {messageName}();
}}
");
		int minSize = fields.CalculateMinSize();
		methodBody.AppendLine($@"
if ({DeserializeParameterName}.Length < {minSize})
{{
    var paddedPayload = new byte[{minSize}];
    Array.Copy({DeserializeParameterName}, paddedPayload, {DeserializeParameterName}.Length);
    {DeserializeParameterName} = paddedPayload;
}}
");

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
		methodBody.AppendLine($@"
if ({DeserializeParameterName}.Length == 0)
{{
    return new {messageName}();
}}
if ({DeserializeParameterName}.Length < {finalSize})
{{
    var paddedPayload = new byte[{finalSize}];
    Array.Copy({DeserializeParameterName}, paddedPayload, {DeserializeParameterName}.Length);
    {DeserializeParameterName} = paddedPayload;
}}
");
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

	private void AppendAssignments(StringBuilder sb, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields, string currentNamespace)
	{
		var assignments = string.Join(",\n", fields.Select(field =>
		{
			var varName = GetVariableName(field.GeneratedName);
			if (field.Type is GeneratedMavlinkMessageFieldEnumType enumField)
			{
				if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
				{
					return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
				}
				else
				{
					string enumTypeName = GetQualifiedEnumTypeName(enumField, currentNamespace);
					return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}Enum";
				}
			}
			return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
		}));
		sb.AppendLine($@"
return new {messageName}
{{
    {assignments}
}};
");
	}

	private void HandleOptionalFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string @namespace)
	{
		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			if (ShouldDeserializeField(field))
			{
				AppendFieldDeserialization(sb, field, ref offset, @namespace);
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
			sb.AppendLine($"var {varName} = {DeserializeParameterName}[{offset}];");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"var {varName} = (sbyte){DeserializeParameterName}[{offset}];");
		}
		else
		{
			string bcMethod = GetBitConverterMethod(typeName);
			sb.AppendLine($"var {varName} = BitConverter.{bcMethod}({DeserializeParameterName}, {offset});");
		}
		offset += size;
	}

	private void AppendEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = size * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);
			if (enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte")
			{
				sb.AppendLine($"var {varName}Value = {DeserializeParameterName}[{offset}];");
				sb.AppendLine($"var combined = ({combinedType}){varName}Value;");
			}
			else
			{
				string bcMethod = GetBitConverterMethod(enumType.ConvertedType);
				sb.AppendLine($"var {varName}Value = BitConverter.{bcMethod}({DeserializeParameterName}, {offset});");
				sb.AppendLine($"var combined = ({combinedType}){varName}Value;");
			}
			sb.AppendLine($@"
var temp{varName} = new List<{enumTypeName}>();
for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)
{{
    if ((combined & (({combinedType})1 << bit_{varName})) != 0)
    {{
        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));
    }}
}}
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
		}
		else
		{
			if (enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte")
			{
				sb.AppendLine($"var {varName}Value = {DeserializeParameterName}[{offset}];");
				sb.AppendLine($"if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out var {varName}Enum))");
				sb.AppendLine("{");
				sb.AppendLine($"    throw new InvalidDataException($\"Invalid enum value {{ {varName}Value }} for {enumTypeName}\");");
				sb.AppendLine("}");
			}
			else
			{
				string bcMethod = GetBitConverterMethod(enumType.ConvertedType);
				sb.AppendLine($"var {varName}Value = BitConverter.{bcMethod}({DeserializeParameterName}, {offset});");
				sb.AppendLine($"if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out var {varName}Enum))");
				sb.AppendLine("{");
				sb.AppendLine($"    throw new InvalidDataException($\"Invalid enum value {{ {varName}Value }} for {enumTypeName}\");");
				sb.AppendLine("}");
			}
		}
		offset += size;
	}

	private void AppendArrayFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int arrayByteLength = arrayType.ArrayLength * elementSize;
		sb.AppendLine($@"
var temp{varName} = new {arrayType.ConvertedType}[{arrayType.ArrayLength}];
Buffer.BlockCopy({DeserializeParameterName}, {offset}, temp{varName}, 0, {arrayByteLength});
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
		offset += arrayByteLength;
	}

	private void AppendArrayEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		var arrayEnumType = (GeneratedMavlinkMessageFieldArrayEnumType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int arrayByteLength = arrayEnumType.ArrayLength * elementSize;
		const int BitsPerByte = 8;
		int shift = elementSize * BitsPerByte;
		int totalBits = arrayEnumType.ArrayLength * elementSize * BitsPerByte;
		string combinedType = GetCombinedTypeForTotalBits(totalBits);
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			string innerCode;
			if (arrayEnumType.ConvertedType == "byte")
			{
				innerCode = $@"combined |= (({combinedType}){DeserializeParameterName}[{offset} + idx_{varName} * {elementSize}])
        << (idx_{varName} * {BitsPerByte});";
			}
			else if (arrayEnumType.ConvertedType == "sbyte")
			{
				innerCode = $@"combined |= (({combinedType})(byte){DeserializeParameterName}[{offset} + idx_{varName} * {elementSize}])
        << (idx_{varName} * {BitsPerByte});";
			}
			else if (arrayEnumType.ConvertedType == "char")
			{
				innerCode = $@"combined |= (({combinedType})BitConverter.ToUInt16({DeserializeParameterName}, {offset} + idx_{varName} * {elementSize}))
        << (idx_{varName} * {elementSize * 8});";
			}
			else
			{
				innerCode = $@"combined |= (({combinedType})BitConverter.{GetBitConverterMethod(arrayEnumType.ConvertedType)}({DeserializeParameterName}, {offset} + idx_{varName} * {elementSize}))
        << (idx_{varName} * {shift});";
			}

			sb.AppendLine($@"
{combinedType} combined = 0;
for (int idx_{varName} = 0; idx_{varName} < {arrayEnumType.ArrayLength}; idx_{varName}++)
{{
    {innerCode}
}}
var temp{varName} = new List<{enumTypeName}>();
for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)
{{
    if ((combined & (({combinedType})1 << bit_{varName})) != 0)
    {{
        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));
    }}
}}
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
			offset += arrayByteLength;
		}
		else
		{
			sb.AppendLine(GenerateArrayDeserializationLoopEnumWithValidation(arrayEnumType.ConvertedType, arrayEnumType.ArrayLength, offset, varName, enumTypeName));
			sb.AppendLine($"var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});");
			offset += arrayByteLength;
		}
	}

	private string GenerateArrayDeserializationLoopEnumWithValidation(string convertedType, int arrayLength, int baseOffset, string varName, string enumTypeName)
	{
		int size = Utilities.GetDotNetTypeSize(convertedType);
		var sb = new StringBuilder();
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i_{varName} = 0; i_{varName} < {arrayLength}; i_{varName}++)");
		sb.AppendLine("    {");
		if (convertedType == "byte")
		{
			sb.AppendLine($"        var {varName}ElementValue = {DeserializeParameterName}[{baseOffset} + i_{varName} * {size}];");
		}
		else if (convertedType == "sbyte")
		{
			sb.AppendLine($"        var {varName}ElementValue = (sbyte){DeserializeParameterName}[{baseOffset} + i_{varName} * {size}];");
		}
		else
		{
			string bcMethod = GetBitConverterMethod(convertedType);
			string cast = convertedType == "char" ? "(char)" : "";
			sb.AppendLine($"        var {varName}ElementValue = {cast}BitConverter.{bcMethod}({DeserializeParameterName}, {baseOffset} + i_{varName} * {size});");
		}
		sb.AppendLine($@"
        if (!Enum.TryParse<{enumTypeName}>({varName}ElementValue.ToString(), out var {varName}Enum))
        {{
            throw new InvalidDataException($""Invalid enum value {{{varName}ElementValue}} for {enumTypeName}"");
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

	private string GetBitConverterMethod(string typeName)
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
