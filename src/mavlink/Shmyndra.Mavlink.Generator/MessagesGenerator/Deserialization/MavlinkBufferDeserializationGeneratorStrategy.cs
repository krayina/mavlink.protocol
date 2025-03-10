using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Implements a buffer-based deserialization strategy using BitConverter and Buffer.
/// </summary>
public class MavlinkBufferDeserializationGeneratorStrategy : IMavlinkDeserializationGeneratorStrategy
{
	private const int BitsPerByte = 8;

	public void AppendBufferInitialization(StringBuilder sb, string messageName, int requiredSize, string payloadParameterName)
	{
		sb.AppendLine($@"
if ({payloadParameterName}.Length == 0)
{{
    return new {messageName}();
}}
else if ({payloadParameterName}.Length < {requiredSize})
{{
    var paddedPayload = new byte[{requiredSize}];
    Array.Copy({payloadParameterName}, paddedPayload, {payloadParameterName}.Length);
    {payloadParameterName} = paddedPayload;
}}
");
	}

	public string AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		return string.IsNullOrWhiteSpace(field.Original.Invalid)
			? AppendDefaultFieldDeserialization(sb, field, ref offset, currentNamespace, payloadParameterName)
			: AppendValidatedFieldDeserialization(sb, field, ref offset, currentNamespace, payloadParameterName);
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
				return AppendEnumField(sb, enumType, field.Original.Display, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				return AppendArrayEnumField(sb, arrayEnumType, field.Original.Display, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleField(sb, simpleType.ConvertedType, ref offset, originalFieldName, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.Original.Type.GetType().Name}' is not supported.");
		}
	}

	private string AppendValidatedFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string fieldName = Utilities.ToLowerCamelCase(field.GeneratedName);
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType:
				AppendEnumFieldValidated(sb, enumType, handler, ref offset, fieldName, originalFieldName, currentNamespace, payloadParameterName);
				return $"{fieldName}Enum";
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayFieldValidated(sb, arrayType, handler, ref offset, fieldName, originalFieldName, payloadParameterName);
				return $"{fieldName}Array";
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, fieldName, originalFieldName, currentNamespace, payloadParameterName);
				return $"{fieldName}Array";
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				AppendSimpleFieldValidated(sb, simpleType.ConvertedType, handler, ref offset, fieldName, payloadParameterName);
				return fieldName;
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
			sb.AppendLine($"var {safeFieldName} = {payloadParameterName}[{offset}];");
		}
		else if (typeName == "sbyte")
		{
			sb.AppendLine($"var {safeFieldName} = (sbyte){payloadParameterName}[{offset}];");
		}
		else
		{
			sb.AppendLine($"var {safeFieldName} = BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset});");
		}
		offset += size;
		return safeFieldName;
	}

	private void AppendSimpleFieldValidated(StringBuilder sb, string typeName, IInvalidFieldHandler handler, ref int offset, string fieldName, string payloadParameterName)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		string safeFieldName = Utilities.GetSafeVariableName(fieldName, payloadParameterName);
		string safeValueName = Utilities.GetSafeVariableName($"{fieldName}Value", payloadParameterName);
		string valueExpr = typeName == "byte" ? $"{payloadParameterName}[{offset}]" :
						  typeName == "sbyte" ? $"(sbyte){payloadParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
		sb.AppendLine($@"
{typeName}? {safeFieldName} = null;
var {safeValueName} = {valueExpr};
if ({handler.GenerateValidationCondition($"{safeValueName}")})
{{
    {safeFieldName} = {safeValueName};
}}
");
		offset += size;
	}

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, MavlinkMessageFieldDisplay display, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";

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

	private void AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IInvalidFieldHandler handler, ref int offset, string fieldName, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";

		sb.AppendLine($@"
{enumTypeName}? {fieldName}Enum = null;
var {fieldName}Value = {valueExpr};
if ({handler.GenerateValidationCondition($"{fieldName}Value")})
{{
    {fieldName}Enum = ({enumTypeName}){fieldName}Value;");
		if (enumType.GeneratedEnum.Original.Bitmask != true)
		{
			sb.AppendLine($@"
    if (!Enum.IsDefined(typeof({enumTypeName}), {fieldName}Enum))
    {{
        throw new InvalidDataException($""Invalid enum value {{{fieldName}Value}} for {enumTypeName}"");
    }}");
		}
		sb.AppendLine($@"
}}
");
		offset += size;
	}

	private string AppendArrayField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string originalFieldName, string payloadParameterName)
	{
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int totalSize = arrayType.ArrayLength * elementSize;
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");

		sb.AppendLine($@"
var {tempFieldName} = new {arrayType.ConvertedType}[{arrayType.ArrayLength}];
Buffer.BlockCopy({payloadParameterName}, {offset}, {tempFieldName}, 0, {totalSize});
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private void AppendArrayFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, IInvalidFieldHandler handler, ref int offset, string fieldName, string originalFieldName, string payloadParameterName)
	{
		string typeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(typeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = typeName == "byte" ? $"{payloadParameterName}[{offset} + {indexVarName}]" :
						  typeName == "sbyte" ? $"(sbyte){payloadParameterName}[{offset} + {indexVarName}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

		sb.AppendLine($@"
var {fieldName} = new {typeName}?[{arrayType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayType.ArrayLength}; {indexVarName}++)
{{
    var value = {valueExpr};
    {fieldName}[{indexVarName}] = {handler.GenerateValidationCondition("value")} ? value : null;
}}
var {fieldName}Array = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		offset += totalSize;
	}

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, MavlinkMessageFieldDisplay display, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
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
var {tempFieldName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    {combinedType} combined = {(elementTypeName == "byte" ? $"{payloadParameterName}[elementOffset]" :
								   $"BitConverter.{GetBitConverterMethod(elementTypeName)}({payloadParameterName}, elementOffset)")};
    var {tempFlagsName} = new List<{enumTypeName}>();
    for (int {bitVarName} = 0; {bitVarName} < {bitsPerElement}; {bitVarName}++)
    {{
        if ((combined & (({combinedType})1 << {bitVarName})) != 0)
        {{
            {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << {bitVarName}));
        }}
    }}
    {tempFieldName}[{indexVarName}] = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		}
		else
		{
			string valueExpr = elementTypeName == "byte" ? $"{payloadParameterName}[{offset} + {indexVarName}]" :
							  $"BitConverter.{GetBitConverterMethod(elementTypeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

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

	private void AppendArrayEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, IInvalidFieldHandler handler, ref int offset, string fieldName, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = elementTypeName == "byte" ? $"{payloadParameterName}[{offset} + {indexVarName}]" :
						  $"BitConverter.{GetBitConverterMethod(elementTypeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

		sb.AppendLine($@"
var temp{fieldName} = new List<{enumTypeName}>({arrayEnumType.ArrayLength});
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
        temp{fieldName}.Add(enumValue);
    }}
}}
var {fieldName}Array = System.Collections.Immutable.ImmutableArray.CreateRange(temp{fieldName});
");
		offset += totalSize;
	}

	private static string GetBitConverterMethod(string typeName)
	{
		switch (typeName)
		{
			case "int": return "ToInt32";
			case "uint": return "ToUInt32";
			case "short": return "ToInt16";
			case "ushort": return "ToUInt16";
			case "long": return "ToInt64";
			case "ulong": return "ToUInt64";
			case "float": return "ToSingle";
			case "double": return "ToDouble";
			case "byte": return "ToByte";
			case "sbyte": return "ToSByte";
			case "char": return "ToChar";
			default: throw new NotSupportedException($"Unsupported type: {typeName}");
		}
	}
}
