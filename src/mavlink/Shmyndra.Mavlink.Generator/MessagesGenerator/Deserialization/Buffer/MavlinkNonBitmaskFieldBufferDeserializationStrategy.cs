using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkNonBitmaskFieldBufferDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		return string.IsNullOrWhiteSpace(field.Original.Invalid)
			? DeserializeDefaultField(sb, field, ref offset, currentNamespace, payloadParameterName)
			: DeserializeValidatedField(sb, field, ref offset, currentNamespace, payloadParameterName);
	}

	private string DeserializeDefaultField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayEnumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleField(sb, simpleType.ConvertedType, ref offset, originalFieldName, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Non-Bitmask strategy.");
		}
	}

	private string DeserializeValidatedField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string fieldName = Utilities.ToLowerCamelCase(field.GeneratedName);
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendEnumFieldValidated(sb, enumType, handler, ref offset, fieldName, currentNamespace, payloadParameterName);
				return $"{fieldName}Enum";
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				AppendArrayFieldValidated(sb, arrayType, handler, ref offset, fieldName, originalFieldName, payloadParameterName);
				return $"{fieldName}Array";
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, fieldName, originalFieldName, currentNamespace, payloadParameterName);
				return $"{fieldName}Array";
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				AppendSimpleFieldValidated(sb, simpleType.ConvertedType, handler, ref offset, fieldName, payloadParameterName);
				return fieldName;
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Non-Bitmask strategy.");
		}
	}

	private string AppendSimpleField(StringBuilder sb, string typeName, ref int offset, string originalFieldName, string payloadParameterName)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string safeFieldName = Utilities.GetSafeVariableName(fieldName, payloadParameterName);

		if (typeName == "byte")
			sb.AppendLine($"var {safeFieldName} = {payloadParameterName}[{offset}];");
		else if (typeName == "sbyte")
			sb.AppendLine($"var {safeFieldName} = (sbyte){payloadParameterName}[{offset}];");
		else
			sb.AppendLine($"var {safeFieldName} = BitConverter.{MavlinkBufferDeserializationExtensions
				.GetBitConverterMethod(typeName)}({payloadParameterName}, {offset});");

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
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";

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

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
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

	private void AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IInvalidFieldHandler handler, ref int offset, string fieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";

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
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(typeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

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

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = elementTypeName == "byte" ? $"{payloadParameterName}[{offset} + {indexVarName}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(elementTypeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

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
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(elementTypeName)}({payloadParameterName}, {offset} + {indexVarName} * {elementSize})";

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
}
