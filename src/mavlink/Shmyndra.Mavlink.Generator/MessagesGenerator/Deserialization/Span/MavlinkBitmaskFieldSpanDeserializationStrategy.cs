using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBitmaskFieldSpanDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	private const int BitsPerByte = 8;

	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		return string.IsNullOrWhiteSpace(field.Original.Invalid)
			? DeserializeDefaultField(sb, field, ref offset, currentNamespace)
			: DeserializeValidatedField(sb, field, ref offset, currentNamespace);
	}

	private string DeserializeDefaultField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayEnumType, ref offset, originalFieldName, currentNamespace);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private string DeserializeValidatedField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string originalFieldName = field.GeneratedName;
		var handler = InvalidFieldHandlerFactory.Create(field) ?? throw new InvalidOperationException($"No handler for field {field.GeneratedName}");

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumFieldValidated(sb, enumType, handler, ref offset, originalFieldName, currentNamespace);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumFieldValidated(sb, arrayEnumType, handler, ref offset, originalFieldName, currentNamespace);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Bitmask strategy.");
		}
	}

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";
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

	private string AppendEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, IInvalidFieldHandler handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";
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
		return fieldName;
	}

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
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
    for (int bit{originalFieldName} = 0; bit{originalFieldName} < {bitsPerElement}; bit{originalFieldName}++)
    {{
        if ((combined & (({combinedType})1 << bit{originalFieldName})) != 0)
        {{
            {tempFlagsName}.Add(({enumTypeName})(({combinedType})1 << bit{originalFieldName}));
        }}
    }}
    {fieldName}[{indexVarName}] = System.Collections.Immutable.ImmutableArray.CreateRange({tempFlagsName});
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendArrayEnumFieldValidated(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, IInvalidFieldHandler handler, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string indexVarName = $"idx{originalFieldName}";
		int bitsPerElement = elementSize * BitsPerByte;
		string combinedType = Utilities.GetCombinedTypeForTotalBits(bitsPerElement);
		string tempFlagsName = Utilities.ToLowerCamelCase($"tempFlags{originalFieldName}");

		sb.AppendLine($@"
var {fieldName} = new {enumTypeName}?[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = {(elementTypeName == "byte" ? $"span[elementOffset]" :
				 $"System.Buffers.Binary.BinaryPrimitives.{GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice(elementOffset, {elementSize}))")};
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
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({fieldName});
");
		offset += totalSize;
		return arrayFieldName;
	}

	private static string GetBinaryPrimitivesMethod(string typeName) => typeName switch
	{
		"int" => "ReadInt32LittleEndian",
		"uint" => "ReadUInt32LittleEndian",
		"short" => "ReadInt16LittleEndian",
		"ushort" => "ReadUInt16LittleEndian",
		"char" => "ReadUInt16LittleEndian",
		"long" => "ReadInt64LittleEndian",
		"ulong" => "ReadUInt64LittleEndian",
		"float" => "ReadSingleLittleEndian",
		"double" => "ReadDoubleLittleEndian",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
