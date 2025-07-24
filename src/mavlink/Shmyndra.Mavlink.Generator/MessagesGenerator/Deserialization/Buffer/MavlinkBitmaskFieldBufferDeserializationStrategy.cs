using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBitmaskFieldBufferDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	private const int BitsPerByte = 8;

	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
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

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions
						  .GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
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

	private string AppendArrayEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = arrayEnumType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayEnumType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayEnumType.ArrayLength * elementSize;

		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string indexVarName = $"idx{originalFieldName}";

		sb.AppendLine($@"
var {tempFieldName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = BitConverter.{MavlinkBufferDeserializationExtensions.GetBitConverterMethod(elementTypeName)}({payloadParameterName}, elementOffset);
    {tempFieldName}[{indexVarName}] = ({enumTypeName})value;
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");

		offset += totalSize;
		return arrayFieldName;
	}
}
