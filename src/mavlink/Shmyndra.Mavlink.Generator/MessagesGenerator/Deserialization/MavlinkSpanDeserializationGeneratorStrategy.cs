using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Implements a span-based deserialization strategy using BinaryPrimitives and ReadOnlySpan.
/// </summary>
public class MavlinkSpanDeserializationGeneratorStrategy : IMavlinkDeserializationGeneratorStrategy
{
	private const int BitsPerByte = 8;

	public void AppendBufferInitialization(StringBuilder sb, string messageName, int requiredSize, string payloadParameterName)
	{
		sb.AppendLine($@"
if ({payloadParameterName}.Length == 0)
{{
    return new {messageName}();
}}
byte[] local = {payloadParameterName}.Length < {requiredSize} ? new byte[{requiredSize}] : {payloadParameterName};
if ({payloadParameterName}.Length < {requiredSize})
{{
    {payloadParameterName}.CopyTo(local, 0);
}}
ReadOnlySpan<byte> span = local;
");
	}

	public string AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		return string.IsNullOrWhiteSpace(field.Original.Invalid)
			? AppendDefaultFieldDeserialization(sb, field, ref offset, currentNamespace, payloadParameterName)
			: AppendValidatedFieldDeserialization(sb, field, ref offset, currentNamespace);
	}

	public void AppendReturnStatement(StringBuilder sb, string messageName, IDictionary<GeneratedMavlinkMessageField, string> fields)
	{
		var formattedAssignments = string.Join(",\n    ", fields.Select(kvp =>
			$"{Utilities.EscapeReservedKeyword(kvp.Key.GeneratedName)} = {kvp.Value}"));

		sb.AppendLine($@"
return new {messageName}
{{
    {formattedAssignments}
}};");
	}

	private string AppendDefaultFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType:
				return AppendEnumField(sb, enumType, field.Original.Display, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				return AppendArrayEnumField(sb, arrayEnumType, field.Original.Display, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleField(sb, simpleType.ConvertedType, ref offset, originalFieldName, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported.");
		}
	}

	private string AppendValidatedFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType:
				return AppendEnumFieldValidated(sb, enumType, handler, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayFieldValidated(sb, arrayType, handler, ref offset, originalFieldName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				return AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleFieldValidated(sb, simpleType.ConvertedType, handler, ref offset, originalFieldName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported.");
		}
	}

	private string AppendSimpleField(StringBuilder sb, string typeName, ref int offset, string originalFieldName, string payloadParameterName)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string safeFieldName = Utilities.GetSafeVariableName(fieldName, payloadParameterName);

		if (typeName == "byte")
		{
			sb.AppendLine($"var {safeFieldName} = span[{offset}];");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"var {safeFieldName} = (sbyte)span[{offset}];");
		}
		else
		{
			sb.AppendLine($"var {safeFieldName} = System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}));");
		}
		offset += size;
		return safeFieldName;
	}

	private string AppendSimpleFieldValidated(StringBuilder sb, string typeName, IInvalidFieldHandler handler, ref int offset, string originalFieldName)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" ? $"span[{offset}]" :
						  typeName == "sbyte" ? $"(sbyte)span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

		sb.AppendLine($@"
{typeName}? {fieldName} = null;
var {fieldName}Value = {valueExpr};
if ({handler.GenerateValidationCondition($"{fieldName}Value")})
{{
    {fieldName} = {fieldName}Value;
}}
");
		offset += size;
		return fieldName;
	}

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, MavlinkMessageFieldDisplay display, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

		if (display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = size * BitsPerByte;
			string combinedType = Utilities.GetCombinedTypeForTotalBits(totalBits);
			string tempFlagsName = Utilities.ToLowerCamelCase($"tempFlags{originalFieldName}");

			sb.AppendLine($@"
var {fieldName}Value = {valueExpr};
{combinedType} combined = ({combinedType}){fieldName}Value;
var {tempFlagsName} = new List<{enumTypeName}>();
for (int bit{originalFieldName} = 0; bit{originalFieldName} < {totalBits}; bit{originalFieldName}++)
{{
    if ((combined & (({combinedType})1 << bit{originalFieldName})) != 0)
    {{
        {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << bit{originalFieldName}));
    }}
}}
var {fieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
");
			offset += size;
			return fieldName;
		}
		else
		{
			string enumFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Enum");
			sb.AppendLine($@"
var {fieldName}Value = {valueExpr};
var {enumFieldName} = ({enumTypeName}){fieldName}Value;");
			if (enumType.GeneratedEnum.Original.Bitmask != true)
			{
				sb.AppendLine($@"
if (!Enum.IsDefined(typeof({enumTypeName}), {enumFieldName}))
{{
    throw new InvalidDataException($""Invalid enum value {{{fieldName}Value}} for {enumTypeName}"");
}}
");
			}
			offset += size;
			return enumFieldName;
		}
	}

	private string AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IInvalidFieldHandler handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string enumFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Enum");
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

		sb.AppendLine($@"
{enumTypeName}? {enumFieldName} = null;
var {fieldName}Value = {valueExpr};
if ({handler.GenerateValidationCondition($"{fieldName}Value")})
{{
    {enumFieldName} = ({enumTypeName}){fieldName}Value;");
		if (enumType.GeneratedEnum.Original.Bitmask != true)
		{
			sb.AppendLine($@"
    if (!Enum.IsDefined(typeof({enumTypeName}), {enumFieldName}))
    {{
        throw new InvalidDataException($""Invalid enum value {{{fieldName}Value}} for {enumTypeName}"");
    }}");
		}
		sb.AppendLine($@"
}}
");
		offset += size;
		return enumFieldName;
	}

	private string AppendArrayField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string originalFieldName)
	{
		string typeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(typeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";

		sb.AppendLine($@"
var {tempFieldName} = new {typeName}[{arrayType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayType.ArrayLength}; {indexVarName}++)
{{
    {tempFieldName}[{indexVarName}] = {(typeName == "byte" ? $"span[{offset} + {indexVarName}]" :
									   typeName == "sbyte" ? $"(sbyte)span[{offset} + {indexVarName}]" :
									   $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))")};
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendArrayFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, IInvalidFieldHandler handler, ref int offset, string originalFieldName)
	{
		string typeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(typeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = typeName == "byte" ? $"span[{offset} + {indexVarName}]" :
						  typeName == "sbyte" ? $"(sbyte)span[{offset} + {indexVarName}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

		sb.AppendLine($@"
var {fieldName} = new {typeName}?[{arrayType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayType.ArrayLength}; {indexVarName}++)
{{
    var value = {valueExpr};
    {fieldName}[{indexVarName}] = {handler.GenerateValidationCondition("value")} ? value : null;
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, MavlinkMessageFieldDisplay display, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string indexVarName = $"idx{originalFieldName}";
		string bitVarName = $"bit{originalFieldName}";

		if (display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int bitsPerElement = elementSize * BitsPerByte;
			string combinedType = Utilities.GetCombinedTypeForTotalBits(bitsPerElement);
			string tempFlagsName = Utilities.ToLowerCamelCase($"tempFlags{originalFieldName}");

			sb.AppendLine($@"
var {fieldName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    {combinedType} combined = {(elementTypeName == "byte" ? $"span[elementOffset]" :
							   $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice(elementOffset, {elementSize}))")};
    var {tempFlagsName} = new List<{enumTypeName}>();
    for (int {bitVarName} = 0; {bitVarName} < {bitsPerElement}; {bitVarName}++)
    {{
        if ((combined & (({combinedType})1 << {bitVarName})) != 0)
        {{
            {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << {bitVarName}));
        }}
    }}
    {fieldName}[{indexVarName}] = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		}
		else
		{
			string valueExpr = elementTypeName == "byte" ? $"span[{offset} + {indexVarName}]" :
							  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

			sb.AppendLine($@"
var {tempFieldName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    var value = {valueExpr};
    var enumValue = ({enumTypeName})value;");
			if (arrayEnumType.GeneratedEnum.Original.Bitmask != true)
			{
				sb.AppendLine($@"
    if (!Enum.IsDefined(typeof({enumTypeName}), enumValue))
    {{
        throw new InvalidDataException($""Invalid enum value {{value}} for {enumTypeName}"");
    }}");
			}
			sb.AppendLine($@"
    {tempFieldName}[{indexVarName}] = enumValue;
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		}
		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendArrayEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, IInvalidFieldHandler handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = elementTypeName == "byte" ? $"span[{offset} + {indexVarName}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

		sb.AppendLine($@"
var {tempFieldName} = new List<{enumTypeName}>({arrayEnumType.ArrayLength});
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    var value = {valueExpr};
    if ({handler.GenerateValidationCondition("value")})
    {{
        var enumValue = ({enumTypeName})value;");
		if (arrayEnumType.GeneratedEnum.Original.Bitmask != true)
		{
			sb.AppendLine($@"
        if (!Enum.IsDefined(typeof({enumTypeName}), enumValue))
        {{
            throw new InvalidDataException($""Invalid enum value {{value}} for {enumTypeName}"");
        }}");
		}
		sb.AppendLine($@"
        {tempFieldName}.Add(enumValue);
    }}
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private static string GetBinaryPrimitivesMethod(string typeName)
	{
		switch (typeName)
		{
			case "int": return "ReadInt32LittleEndian";
			case "uint": return "ReadUInt32LittleEndian";
			case "short": return "ReadInt16LittleEndian";
			case "ushort": return "ReadUInt16LittleEndian";
			case "char": return "ReadUInt16LittleEndian";
			case "long": return "ReadInt64LittleEndian";
			case "ulong": return "ReadUInt64LittleEndian";
			case "float": return "ReadSingleLittleEndian";
			case "double": return "ReadDoubleLittleEndian";
			default: throw new NotSupportedException($"Unsupported type: {typeName}");
		}
	}
}
