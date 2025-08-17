using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy : IMavlinkFieldDeserializationStrategy
{
	public string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		string originalFieldName = field.GeneratedName;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldPrimitiveType primitiveType:
				return AppendPrimitiveField(sb, primitiveType, ref offset, originalFieldName, payloadParameterName);

			case GeneratedMavlinkMessageFieldArrayType { ElementType: GeneratedMavlinkMessageFieldEnumType enumElementType } arrayType
				when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendArrayEnumField(sb, arrayType, enumElementType, ref offset, originalFieldName, currentNamespace, payloadParameterName);

			case GeneratedMavlinkMessageFieldArrayType arrayType:
				return AppendArrayField(sb, arrayType, ref offset, originalFieldName, payloadParameterName);

			case GeneratedMavlinkMessageFieldEnumType enumType
				when field.Original.Display == MavlinkMessageFieldDisplay.Bitmask:
				return AppendEnumField(sb, enumType, ref offset, originalFieldName, currentNamespace, payloadParameterName);

			default:
				throw new NotSupportedException($"Field type '{field.GeneratedType.GetType().Name}' is not supported in Objective Bitmask Span strategy.");
		}
	}

	private string AppendPrimitiveField(StringBuilder sb, GeneratedMavlinkMessageFieldPrimitiveType primitiveType, ref int offset, string originalFieldName, string payloadParameterName)
	{
		string typeName = primitiveType.ConvertedType;
		int size = Utilities.GetDotNetTypeSize(typeName);
		string fieldName = Utilities.ToLowerCamelCase(originalFieldName);
		string valueExpr = typeName == "byte" || typeName == "sbyte" ? $"{payloadParameterName}[{offset}]" :
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
							.GetBinaryPrimitivesMethod(typeName)}({payloadParameterName}.Slice({offset}, {size}))";
		string bitmaskType = Utilities.GetPrimitiveBitmaskType(typeName);

		sb.AppendLine($@"
var {fieldName} = new {bitmaskType}(({typeName}){valueExpr});
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
				  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
					.GetBinaryPrimitivesMethod(elementTypeName)}({payloadParameterName}.Slice(elementOffset, {elementSize}))")};
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
						  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
							.GetBinaryPrimitivesMethod(typeName)}({payloadParameterName}.Slice({offset}, {size}))";
		string objectiveBitmaskType = $"{enumTypeName}Bitmask";

		sb.AppendLine($@"
var {fieldName} = new {objectiveBitmaskType}(({typeName}){valueExpr});
");

		offset += size;
		return fieldName;
	}

	private string AppendArrayEnumField(
		StringBuilder sb,
		GeneratedMavlinkMessageFieldArrayType arrayType,
		GeneratedMavlinkMessageFieldEnumType enumElementType,
		ref int offset,
		string originalFieldName,
		string currentNamespace,
		string payloadParameterName)
	{
		string enumTypeName = enumElementType.GetQualifiedEnumTypeName(currentNamespace);
		string elementTypeName = arrayType.ConvertedType;
		int elementSize = Utilities.GetDotNetTypeSize(elementTypeName);
		int totalSize = arrayType.ArrayLength * elementSize;
		string arrayFieldName = Utilities.ToLowerCamelCase($"{originalFieldName}Array");
		string tempFieldName = Utilities.ToLowerCamelCase($"temp{originalFieldName}");
		string indexVarName = $"idx{originalFieldName}";
		string objectiveBitmaskType = $"{enumTypeName}Bitmask";

		sb.AppendLine($@"
var {tempFieldName} = new {objectiveBitmaskType}[{arrayType.ArrayLength}];
for (int {indexVarName} = 0; {indexVarName} < {arrayType.ArrayLength}; {indexVarName}++)
{{
    int elementOffset = {offset} + {indexVarName} * {elementSize};
    var value = {(elementTypeName == "byte" ? $"{payloadParameterName}[elementOffset]" :
				  $"System.Buffers.Binary.BinaryPrimitives.{MavlinkSpanDeserializationExtensions
					.GetBinaryPrimitivesMethod(elementTypeName)}({payloadParameterName}.Slice(elementOffset, {elementSize}))")};
    {tempFieldName}[{indexVarName}] = new {objectiveBitmaskType}(({elementTypeName})value);
}}
var {arrayFieldName} = System.Collections.Immutable.ImmutableArray.CreateRange({tempFieldName});
");

		offset += totalSize;
		return arrayFieldName;
	}
}
