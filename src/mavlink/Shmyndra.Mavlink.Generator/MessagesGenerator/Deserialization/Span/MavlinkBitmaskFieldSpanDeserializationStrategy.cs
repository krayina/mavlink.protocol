using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBitmaskFieldSpanDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	private const int BitsPerByte = 8;

	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
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

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"span[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";
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
							   $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions.GetBinaryPrimitivesMethod(elementTypeName)}(span.Slice(elementOffset, {elementSize}))")};
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
}
