using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldPrimitiveType primitiveType:
				return AppendPrimitiveField(sb, primitiveType, ref offset, originalFieldName, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName, payloadParameterName);
			case GeneratedMavlinkMessageFieldEnumType enumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayEnumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);
			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Objective Bitmask strategy.");
		}
	}

	private string AppendPrimitiveField(StringBuilder sb, GeneratedMavlinkMessageFieldPrimitiveType primitiveType, ref int offset, string originalFieldName, string payloadParameterName)
	{
		string typeName = primitiveType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions.GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
		string bitmaskType = Utilities.GetPrimitiveBitmaskType(typeName);

		sb.AppendLine($@"
var {fieldName}Value = {valueExpr};
var {fieldName} = new {bitmaskType}(({typeName}){fieldName}Value);
");

		offset += size;
		return fieldName;
	}

	private string AppendArrayField(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string originalFieldName, string payloadParameterName)
	{
		string elementTypeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string indexVarName = $"idx{originalFieldName}";
		string bitmaskType = Utilities.GetPrimitiveBitmaskType(elementTypeName);

		sb.AppendLine($@"
var {tempFieldName} = new {bitmaskType}[{arrayType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = {(elementTypeName == "byte" ? $"{payloadParameterName}[elementOffset]" :
				  $"BitConverter.{MavlinkBufferDeserializationExtensions.GetBitConverterMethod(elementTypeName)}({payloadParameterName}, elementOffset)")};
    {tempFieldName}[{indexVarName}] = new {bitmaskType}(({elementTypeName})value);
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");

		offset += totalSize;
		return arrayFieldName;
	}

	private string AppendEnumField(StringBuilder sb, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string originalFieldName, string currentNamespace, string payloadParameterName)
	{
		string enumTypeName = enumType.GetQualifiedEnumTypeName(currentNamespace);
		string typeName = enumType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"BitConverter.{MavlinkBufferDeserializationExtensions.GetBitConverterMethod(typeName)}({payloadParameterName}, {offset})";
		string objectiveBitmaskType = $"{enumTypeName}Bitmask";

		sb.AppendLine($@"
var {fieldName}Value = {valueExpr};
var {fieldName} = new {objectiveBitmaskType}(({typeName}){fieldName}Value);
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
		string objectiveBitmaskType = $"{enumTypeName}Bitmask";

		sb.AppendLine($@"
var {tempFieldName} = new {objectiveBitmaskType}[{arrayEnumType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayEnumType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = {(elementTypeName == "byte" ? $"{payloadParameterName}[elementOffset]" :
				  $"BitConverter.{MavlinkBufferDeserializationExtensions.GetBitConverterMethod(elementTypeName)}({payloadParameterName}, elementOffset)")};
    {tempFieldName}[{indexVarName}] = new {objectiveBitmaskType}(({elementTypeName})value);
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");

		offset += totalSize;
		return arrayFieldName;
	}
}
