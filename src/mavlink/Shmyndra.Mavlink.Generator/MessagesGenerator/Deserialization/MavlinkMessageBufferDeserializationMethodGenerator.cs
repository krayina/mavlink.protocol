using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete implementation of <see cref="MavlinkMessageDeserializationMethodGeneratorBase"/> that generates
/// buffer-based deserialization methods for Mavlink messages using <see cref="BitConverter"/> and <see cref="Buffer"/>.
/// This class supports both default deserialization (without validation) and validation-based deserialization
/// for fields with an 'invalid' attribute.
/// </summary>
public class MavlinkMessageBufferDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGeneratorBase
{
	/// <summary>
	/// Appends the prologue for buffer-based deserialization, ensuring the payload meets the required size
	/// and padding it with zeros if necessary.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the prologue code to.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="finalSize">The minimum required size of the payload.</param>
	protected override void AppendMethodPrologue(StringBuilder sb, string messageName, int finalSize)
	{
		sb.AppendLine($@"
if ({DeserializeParameterName}.Length == 0)
{{
    return new {messageName}();
}}
if ({DeserializeParameterName}.Length < {finalSize})
{{
    var paddedPayload = new byte[{finalSize}];
    Array.Copy({DeserializeParameterName}, paddedPayload, {DeserializeParameterName}.Length);
    {DeserializeParameterName} = paddedPayload;
}}
");
	}

	/// <summary>
	/// Appends default deserialization logic for a simple (primitive) field without validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing simple type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected override void AppendSimpleFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName)
	{
		var simpleType = (GeneratedMavlinkMessageFieldType)field.Type;
		int size = Utilities.GetDotNetTypeSize(simpleType.ConvertedType);
		string typeName = simpleType.ConvertedType;
		AppendPrimitiveFieldDeserialization(sb, varName, size, typeName, ref offset);
	}

	/// <summary>
	/// Appends deserialization logic for a simple (primitive) field with validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing simple type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected override void AppendSimpleFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName)
	{
		var simpleType = (GeneratedMavlinkMessageFieldType)field.Type;
		int size = Utilities.GetDotNetTypeSize(simpleType.ConvertedType);
		string typeName = simpleType.ConvertedType;
		string valueExpr = typeName == "byte" ? $"{DeserializeParameterName}[{offset}]" :
						  typeName == "sbyte" ? $"(sbyte){DeserializeParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(typeName)}({DeserializeParameterName}, {offset})";

		sb.AppendLine($"{typeName}? {varName} = null;");
		sb.AppendLine($"var {varName}Value = {valueExpr};");
		string condition = handler.GenerateValidationCondition($"{varName}Value");
		sb.AppendLine($"if ({condition})");
		sb.AppendLine("{");
		sb.AppendLine($"    {varName} = {varName}Value;");
		sb.AppendLine("}");
		offset += size;
	}

	/// <summary>
	/// Appends default deserialization logic for an enum field without validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected override void AppendEnumFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			int totalBits = size * 8;
			string combinedType = GetCombinedTypeForTotalBits(totalBits);
			if (enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte")
			{
				sb.AppendLine($"var {varName}Value = {DeserializeParameterName}[{offset}];");
				sb.AppendLine($"var combined = ({combinedType}){varName}Value;");
			}
			else
			{
				string bcMethod = GetBitConverterMethod(enumType.ConvertedType);
				sb.AppendLine($"var {varName}Value = BitConverter.{bcMethod}({DeserializeParameterName}, {offset});");
				sb.AppendLine($"var combined = ({combinedType}){varName}Value;");
			}
			sb.AppendLine($@"
var temp{varName} = new List<{enumTypeName}>();
for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)
{{
    if ((combined & (({combinedType})1 << bit_{varName})) != 0)
    {{
        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));
    }}
}}
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
		}
		else
		{
			string valueExpr = enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte" ? $"{DeserializeParameterName}[{offset}]" :
							   $"BitConverter.{GetBitConverterMethod(enumType.ConvertedType)}({DeserializeParameterName}, {offset})";
			sb.AppendLine($"var {varName}Value = {valueExpr};");
			sb.AppendLine($"if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out var {varName}Enum))");
			sb.AppendLine("{");
			sb.AppendLine($"    throw new InvalidDataException($\"Invalid enum value {{ {varName}Value }} for {enumTypeName}\");");
			sb.AppendLine("}");
		}
		offset += size;
	}

	/// <summary>
	/// Appends deserialization logic for an enum field with validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected override void AppendEnumFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName, string currentNamespace)
	{
		var enumType = (GeneratedMavlinkMessageFieldEnumType)field.Type;
		string enumTypeName = GetQualifiedEnumTypeName(enumType, currentNamespace);
		int size = Utilities.GetDotNetTypeSize(enumType.ConvertedType);
		string valueExpr = enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte" ? $"{DeserializeParameterName}[{offset}]" :
						  $"BitConverter.{GetBitConverterMethod(enumType.ConvertedType)}({DeserializeParameterName}, {offset})";

		sb.AppendLine($"{enumTypeName}? {varName}Enum = null;");
		sb.AppendLine($"var {varName}Value = {valueExpr};");
		string condition = handler.GenerateValidationCondition($"{varName}Value");
		sb.AppendLine($"if ({condition})");
		sb.AppendLine("{");
		sb.AppendLine($"    if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out {varName}Enum))");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        throw new InvalidDataException($\"Invalid enum value {{ {varName}Value }} for {enumTypeName}\");");
		sb.AppendLine($"    }}");
		sb.AppendLine("}");
		offset += size;
	}

	/// <summary>
	/// Appends default deserialization logic for an array field without validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected override void AppendArrayFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName)
	{
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int arrayByteLength = arrayType.ArrayLength * elementSize;
		sb.AppendLine($@"
var temp{varName} = new {arrayType.ConvertedType}[{arrayType.ArrayLength}];
Buffer.BlockCopy({DeserializeParameterName}, {offset}, temp{varName}, 0, {arrayByteLength});
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends deserialization logic for an array field with validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected override void AppendArrayFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName)
	{
		var arrayType = (GeneratedMavlinkMessageFieldArrayType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayType.ConvertedType);
		int arrayByteLength = arrayType.ArrayLength * elementSize;

		sb.AppendLine($"var temp{varName} = new List<{arrayType.ConvertedType}>({arrayType.ArrayLength});");
		sb.AppendLine($"for (int i_{varName} = 0; i_{varName} < {arrayType.ArrayLength}; i_{varName}++)");
		sb.AppendLine("{");
		string valueExpr = arrayType.ConvertedType == "byte" ? $"{DeserializeParameterName}[{offset} + i_{varName} * {elementSize}]" :
						  $"BitConverter.{GetBitConverterMethod(arrayType.ConvertedType)}({DeserializeParameterName}, {offset} + i_{varName} * {elementSize})";
		sb.AppendLine($"    var value = {valueExpr};");
		string condition = handler.GenerateValidationCondition("value");
		sb.AppendLine($"    if ({condition})");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        temp{varName}.Add(value);");
		sb.AppendLine($"    }}");
		sb.AppendLine("}");
		sb.AppendLine($"var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends default deserialization logic for an array of enums without validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected override void AppendArrayEnumFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		var arrayEnumType = (GeneratedMavlinkMessageFieldArrayEnumType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int arrayByteLength = arrayEnumType.ArrayLength * elementSize;
		const int BitsPerByte = 8;
		int shift = elementSize * BitsPerByte;
		int totalBits = arrayEnumType.ArrayLength * elementSize * BitsPerByte;
		string combinedType = GetCombinedTypeForTotalBits(totalBits);
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			string innerCode = arrayEnumType.ConvertedType == "byte" ?
				$@"combined |= (({combinedType}){DeserializeParameterName}[{offset} + idx_{varName} * {elementSize}]) << (idx_{varName} * {BitsPerByte});" :
				arrayEnumType.ConvertedType == "sbyte" ?
				$@"combined |= (({combinedType})(byte){DeserializeParameterName}[{offset} + idx_{varName} * {elementSize}]) << (idx_{varName} * {BitsPerByte});" :
				arrayEnumType.ConvertedType == "char" ?
				$@"combined |= (({combinedType})BitConverter.ToUInt16({DeserializeParameterName}, {offset} + idx_{varName} * {elementSize})) << (idx_{varName} * {elementSize * 8});" :
				$@"combined |= (({combinedType})BitConverter.{GetBitConverterMethod(arrayEnumType.ConvertedType)}({DeserializeParameterName}, {offset} + idx_{varName} * {elementSize})) << (idx_{varName} * {shift});";

			sb.AppendLine($@"
{combinedType} combined = 0;
for (int idx_{varName} = 0; idx_{varName} < {arrayEnumType.ArrayLength}; idx_{varName}++)
{{
    {innerCode}
}}
var temp{varName} = new List<{enumTypeName}>();
for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)
{{
    if ((combined & (({combinedType})1 << bit_{varName})) != 0)
    {{
        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));
    }}
}}
var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});
");
		}
		else
		{
			sb.AppendLine($"var temp{varName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];");
			sb.AppendLine($"for (int i_{varName} = 0; i_{varName} < {arrayEnumType.ArrayLength}; i_{varName}++)");
			sb.AppendLine("{");
			string valueExpr = arrayEnumType.ConvertedType == "byte" ? $"{DeserializeParameterName}[{offset} + i_{varName} * {elementSize}]" :
							  $"BitConverter.{GetBitConverterMethod(arrayEnumType.ConvertedType)}({DeserializeParameterName}, {offset} + i_{varName} * {elementSize})";
			sb.AppendLine($"    var value = {valueExpr};");
			sb.AppendLine($"    if (!Enum.TryParse<{enumTypeName}>(value.ToString(), out var enumValue))");
			sb.AppendLine($"    {{");
			sb.AppendLine($"        throw new InvalidDataException($\"Invalid enum value {{value}} for {enumTypeName}\");");
			sb.AppendLine($"    }}");
			sb.AppendLine($"    temp{varName}[i_{varName}] = enumValue;");
			sb.AppendLine("}");
			sb.AppendLine($"var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});");
		}
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends deserialization logic for an array of enums with validation using buffer-based methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected override void AppendArrayEnumFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName, string currentNamespace)
	{
		var arrayEnumType = (GeneratedMavlinkMessageFieldArrayEnumType)field.Type;
		int elementSize = Utilities.GetDotNetTypeSize(arrayEnumType.ConvertedType);
		int arrayByteLength = arrayEnumType.ArrayLength * elementSize;
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;

		sb.AppendLine($"var temp{varName} = new List<{enumTypeName}>({arrayEnumType.ArrayLength});");
		sb.AppendLine($"for (int i_{varName} = 0; i_{varName} < {arrayEnumType.ArrayLength}; i_{varName}++)");
		sb.AppendLine("{");
		string valueExpr = arrayEnumType.ConvertedType == "byte" ? $"{DeserializeParameterName}[{offset} + i_{varName} * {elementSize}]" :
						  $"BitConverter.{GetBitConverterMethod(arrayEnumType.ConvertedType)}({DeserializeParameterName}, {offset} + i_{varName} * {elementSize})";
		sb.AppendLine($"    var value = {valueExpr};");
		string condition = handler.GenerateValidationCondition("value");
		sb.AppendLine($"    if ({condition})");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        if (!Enum.TryParse<{enumTypeName}>(value.ToString(), out var enumValue))");
		sb.AppendLine($"        {{");
		sb.AppendLine($"            throw new InvalidDataException($\"Invalid enum value {{value}} for {enumTypeName}\");");
		sb.AppendLine($"        }}");
		sb.AppendLine($"        temp{varName}.Add(enumValue);");
		sb.AppendLine($"    }}");
		sb.AppendLine("}");
		sb.AppendLine($"var {varName} = System.Collections.Immutable.ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	private void AppendPrimitiveFieldDeserialization(StringBuilder sb, string varName, int size, string typeName, ref int offset)
	{
		if (typeName == "byte")
			sb.AppendLine($"var {varName} = {DeserializeParameterName}[{offset}];");
		else if (typeName == "sbyte")
			sb.AppendLine($"var {varName} = (sbyte){DeserializeParameterName}[{offset}];");
		else
			sb.AppendLine($"var {varName} = BitConverter.{GetBitConverterMethod(typeName)}({DeserializeParameterName}, {offset});");
		offset += size;
	}

	private string GetBitConverterMethod(string typeName)
	{
		return typeName switch
		{
			"int" => "ToInt32",
			"uint" => "ToUInt32",
			"short" => "ToInt16",
			"ushort" => "ToUInt16",
			"long" => "ToInt64",
			"ulong" => "ToUInt64",
			"float" => "ToSingle",
			"double" => "ToDouble",
			"sbyte" => "ToSByte",
			"byte" => "ToByte",
			"char" => "ToChar",
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}
}
