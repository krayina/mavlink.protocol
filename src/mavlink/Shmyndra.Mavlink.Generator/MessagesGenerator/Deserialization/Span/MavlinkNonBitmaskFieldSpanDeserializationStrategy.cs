using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkNonBitmaskFieldSpanDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		return string.IsNullOrWhiteSpace(field.Original.Invalid)
			? DeserializeDefaultField(sb, field, ref offset, currentNamespace, payloadParameterName)
			: DeserializeValidatedField(sb, field, ref offset, currentNamespace);
	}

	private string DeserializeDefaultField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayEnumType, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleField(sb, simpleType.ConvertedType, ref offset, originalFieldName, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Non-Bitmask strategy.");
		}
	}

	private string DeserializeValidatedField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumFieldValidated(sb, enumType, handler, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayFieldValidated(sb, arrayType, handler, ref offset, originalFieldName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display != MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return AppendSimpleFieldValidated(sb, simpleType.ConvertedType, handler, ref offset, originalFieldName);
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
			sb.AppendLine($"var {safeFieldName} = span[{offset}];");
		else if (typeName == "sbyte")
			sb.AppendLine($"var {safeFieldName} = (sbyte)span[{offset}];");
		else
			sb.AppendLine($"var {safeFieldName} = System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.
				GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}));");

		offset += size;
		return safeFieldName;
	}

	private string AppendSimpleFieldValidated(StringBuilder sb, string typeName, IValidationConditionProvider handler, ref int offset, string originalFieldName)
	{
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" ? $"span[{offset}]" :
						  typeName == "sbyte" ? $"(sbyte)span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.
						  GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

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

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.
						  GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";
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

	private string AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IValidationConditionProvider handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string enumFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Enum");
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
						  .GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

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
									   $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
									   .GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))")};
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendArrayFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, IValidationConditionProvider handler, ref int offset, string originalFieldName)
	{
		string typeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(typeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = typeName == "byte" ? $"span[{offset} + {indexVarName}]" :
						  typeName == "sbyte" ? $"(sbyte)span[{offset} + {indexVarName}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

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

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = elementTypeName == "byte" ? $"span[{offset} + {indexVarName}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
						  .GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

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

	private string AppendArrayEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, IValidationConditionProvider handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		string valueExpr = elementTypeName == "byte" ? $"span[{offset} + {indexVarName}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice({offset} + {indexVarName} * {elementSize}, {elementSize}))";

		sb.AppendLine($@"
var {fieldName} = new {enumTypeName}?[{arrayEnumType.ArrayLength}];
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
        {fieldName}[{indexVarName}] = enumValue;
    }}
    else
    {{
        {fieldName}[{indexVarName}] = null;
    }}
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");

		offset += totalSize;
		return arrayFieldName;
	}
}
