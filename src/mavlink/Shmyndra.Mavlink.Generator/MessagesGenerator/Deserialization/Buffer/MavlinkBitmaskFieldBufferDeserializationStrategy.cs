using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBitmaskFieldBufferDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	private const int BitsPerByte = 8;

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
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayEnumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private string DeserializeValidatedField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string fieldName = Utilities.ToLowerCamelCase(field.GeneratedName);
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendEnumFieldValidated(sb, enumType, handler, ref offset, fieldName, originalFieldName, currentNamespace, payloadParameterName);
				return fieldName;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, fieldName, originalFieldName, currentNamespace, payloadParameterName);
				return $"{fieldName}Array";
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
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

	private void AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IInvalidFieldHandler handler, ref int offset, string fieldName, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
		int totalBits = size * BitsPerByte;
		string combinedType = Utilities.GetCombinedTypeForTotalBits(totalBits);
		string tempFlagsName = Utilities.ToLowerCamelCase($"tempFlags{originalFieldName}");

		sb.AppendLine($@"
{enumTypeName}[]? {fieldName} = null;
var {fieldName}Value = {valueExpr};
if ({handler.GenerateValidationCondition($"{fieldName}Value")})
{{
    {combinedType} combined = ({combinedType}){fieldName}Value;
    var {tempFlagsName} = new List<{enumTypeName}>();
    for (int bit{originalFieldName} = 0; bit{originalFieldName} < {totalBits}; bit{originalFieldName}++)
    {{
        if ((combined & (({combinedType})1 << bit{originalFieldName})) != 0)
        {{
            {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << bit{originalFieldName}));
        }}
    }}
    {fieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName}).ToArray();
}}
");
		offset += size;
	}

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string indexVarName = $"idx{originalFieldName}";
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
    for (int bit{originalFieldName} = 0; bit{originalFieldName} < {bitsPerElement}; bit{originalFieldName}++)
    {{
        if ((combined & (({combinedType})1 << bit{originalFieldName})) != 0)
        {{
            {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << bit{originalFieldName}));
        }}
    }}
    {tempFieldName}[{indexVarName}] = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
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
		int bitsPerElement = elementSize * BitsPerByte;
		string combinedType = Utilities.GetCombinedTypeForTotalBits(bitsPerElement);
		string tempFlagsName = Utilities.ToLowerCamelCase($"tempFlags{originalFieldName}");

		sb.AppendLine($@"
var {fieldName} = new {enumTypeName}?[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = {(elementTypeName == "byte" ? $"{payloadParameterName}[elementOffset]" :
				 $"BitConverter.{GetBitConverterMethod(elementTypeName)}({payloadParameterName}, elementOffset)")};
    if ({handler.GenerateValidationCondition("value")})
    {{
        {combinedType} combined = ({combinedType})value;
        var {tempFlagsName} = new List<{enumTypeName}>();
        for (int bit{originalFieldName} = 0; bit{originalFieldName} < {bitsPerElement}; bit{originalFieldName}++)
        {{
            if ((combined & (({combinedType})1 << bit{originalFieldName})) != 0)
            {{
                {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << bit{originalFieldName}));
            }}
        }}
        {fieldName}[{indexVarName}] = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
    }}
}}
var {fieldName}Array = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		offset += totalSize;
	}

	private static string GetBitConverterMethod(string typeName) => typeName switch
	{
		"int" => "ToInt32",
		"uint" => "ToUInt32",
		"short" => "ToInt16",
		"ushort" => "ToUInt16",
		"long" => "ToInt64",
		"ulong" => "ToUInt64",
		"float" => "ToSingle",
		"double" => "ToDouble",
		"byte" => "ToByte",
		"sbyte" => "ToSByte",
		"char" => "ToChar",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
