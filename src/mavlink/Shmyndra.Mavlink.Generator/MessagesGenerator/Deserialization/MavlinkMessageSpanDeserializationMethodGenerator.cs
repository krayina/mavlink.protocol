using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete implementation of <see cref="MavlinkMessageDeserializationMethodGeneratorBase"/> that generates
/// span-based deserialization methods for Mavlink messages using <see cref="BinaryPrimitives"/> and <see cref="ReadOnlySpan{T}"/>.
/// This class supports both default deserialization (without validation) and validation-based deserialization
/// for fields with an 'invalid' attribute.
/// </summary>
public class MavlinkMessageSpanDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGeneratorBase
{
	/// <summary>
	/// Appends the prologue for span-based deserialization, ensuring the payload meets the required size
	/// and converting it to a <see cref="ReadOnlySpan{T}"/> with padding if necessary.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the prologue code to.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="minSize">The minimum required size of the payload.</param>
	protected override void AppendMethodPrologue(StringBuilder sb, string messageName, int minSize)
	{
		sb.AppendLine($@"
if ({DeserializeParameterName}.Length == 0)
{{
    return new {messageName}();
}}

byte[] local = {DeserializeParameterName}.Length < {minSize} ? new byte[{minSize}] : {DeserializeParameterName};
if ({DeserializeParameterName}.Length < {minSize})
{{
    {DeserializeParameterName}.CopyTo(local, 0);
}}
ReadOnlySpan<byte> span = local;");
	}

	/// <summary>
	/// Appends default deserialization logic for a simple (primitive) field without validation using span-based methods.
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
	/// Appends deserialization logic for a simple (primitive) field with validation using span-based methods.
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
		string valueExpr = typeName == "byte" ? $"span[{offset}]" :
						  typeName == "sbyte" ? $"(sbyte)span[{offset}]" :
						  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}))";

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
	/// Appends default deserialization logic for an enum field without validation using span-based methods.
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
			string combinedType = Utilities.GetCombinedTypeForTotalBits(totalBits);
			string valueExpr = enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte" ? $"span[{offset}]" :
							  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(enumType.ConvertedType)}(span.Slice({offset}, {size}))";

			sb.AppendLine($@"
var {varName}Value = {valueExpr};
{combinedType} combined = ({combinedType}){varName}Value;
var temp{varName} = new List<{enumTypeName}>();
for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)
{{
    if ((combined & (({combinedType})1 << bit_{varName})) != 0)
    {{
        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));
    }}
}}
var {varName} = ImmutableArray.CreateRange(temp{varName});
");
		}
		else
		{
			string valueExpr = enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte" ? $"span[{offset}]" :
							  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(enumType.ConvertedType)}(span.Slice({offset}, {size}))";
			sb.AppendLine($"var {varName}Value = {valueExpr};");
			sb.AppendLine($"if (!Enum.TryParse<{enumTypeName}>({varName}Value.ToString(), out var {varName}Enum))");
			sb.AppendLine("{");
			sb.AppendLine($"    throw new InvalidDataException($\"Invalid enum value {{ {varName}Value }} for {enumTypeName}\");");
			sb.AppendLine("}");
		}
		offset += size;
	}

	/// <summary>
	/// Appends deserialization logic for an enum field with validation using span-based methods.
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
		string valueExpr = enumType.ConvertedType == "byte" || enumType.ConvertedType == "sbyte" ? $"span[{offset}]" :
						  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(enumType.ConvertedType)}(span.Slice({offset}, {size}))";

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
	/// Appends default deserialization logic for an array field without validation using span-based methods.
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
		string loopCode = GenerateArrayDeserializationLoopSimple(arrayType.ConvertedType, arrayType.ArrayLength, offset, varName);
		sb.AppendLine(loopCode);
		sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends deserialization logic for an array field with validation using span-based methods.
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
		string valueExpr = arrayType.ConvertedType == "byte" ? $"span[{offset} + i_{varName} * {elementSize}]" :
						  arrayType.ConvertedType == "sbyte" ? $"(sbyte)span[{offset} + i_{varName} * {elementSize}]" :
						  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(arrayType.ConvertedType)}(span.Slice({offset} + i_{varName} * {elementSize}, {elementSize}))";
		sb.AppendLine($"    var value = {valueExpr};");
		string condition = handler.GenerateValidationCondition("value");
		sb.AppendLine($"    if ({condition})");
		sb.AppendLine($"    {{");
		sb.AppendLine($"        temp{varName}.Add(value);");
		sb.AppendLine($"    }}");
		sb.AppendLine("}");
		sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends default deserialization logic for an array of enums without validation using span-based methods.
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
		string combinedType = Utilities.GetCombinedTypeForTotalBits(totalBits);
		string enumTypeName = arrayEnumType.GeneratedEnum.GeneratedName;

		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			sb.AppendLine($"{combinedType} combined = 0;");
			sb.AppendLine($"for (int idx_{varName} = 0; idx_{varName} < {arrayEnumType.ArrayLength}; idx_{varName}++)");
			sb.AppendLine("{");
			sb.AppendLine($"    int elementOffset = {offset} + idx_{varName} * {elementSize};");
			string valueExpr = arrayEnumType.ConvertedType == "byte" ? $"span[elementOffset]" :
							  arrayEnumType.ConvertedType == "sbyte" ? $"(byte)span[elementOffset]" :
							  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(arrayEnumType.ConvertedType)}(span.Slice(elementOffset, {elementSize}))";
			sb.AppendLine($"    combined |= (({combinedType}){valueExpr}) << (idx_{varName} * {shift});");
			sb.AppendLine("}");
			sb.AppendLine($"var temp{varName} = new List<{enumTypeName}>();");
			sb.AppendLine($"for (int bit_{varName} = 0; bit_{varName} < {totalBits}; bit_{varName}++)");
			sb.AppendLine("{");
			sb.AppendLine($"    if ((combined & (({combinedType})1 << bit_{varName})) != 0)");
			sb.AppendLine($"    {{");
			sb.AppendLine($"        temp{varName}.Add(({enumTypeName})(({combinedType})1 << bit_{varName}));");
			sb.AppendLine($"    }}");
			sb.AppendLine("}");
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		}
		else
		{
			sb.AppendLine($"var temp{varName} = new {enumTypeName}[{arrayEnumType.ArrayLength}];");
			sb.AppendLine($"for (int i_{varName} = 0; i_{varName} < {arrayEnumType.ArrayLength}; i_{varName}++)");
			sb.AppendLine("{");
			string valueExpr = arrayEnumType.ConvertedType == "byte" ? $"span[{offset} + i_{varName} * {elementSize}]" :
							  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(arrayEnumType.ConvertedType)}(span.Slice({offset} + i_{varName} * {elementSize}, {elementSize}))";
			sb.AppendLine($"    var value = {valueExpr};");
			sb.AppendLine($"    if (!Enum.TryParse<{enumTypeName}>(value.ToString(), out var enumValue))");
			sb.AppendLine($"    {{");
			sb.AppendLine($"        throw new InvalidDataException($\"Invalid enum value {{value}} for {enumTypeName}\");");
			sb.AppendLine($"    }}");
			sb.AppendLine($"    temp{varName}[i_{varName}] = enumValue;");
			sb.AppendLine("}");
			sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		}
		offset += arrayByteLength;
	}

	/// <summary>
	/// Appends deserialization logic for an array of enums with validation using span-based methods.
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
		string valueExpr = arrayEnumType.ConvertedType == "byte" ? $"span[{offset} + i_{varName} * {elementSize}]" :
						  arrayEnumType.ConvertedType == "sbyte" ? $"(sbyte)span[{offset} + i_{varName} * {elementSize}]" :
						  $"BinaryPrimitives.{GetBinaryPrimitivesMethod(arrayEnumType.ConvertedType)}(span.Slice({offset} + i_{varName} * {elementSize}, {elementSize}))";
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
		sb.AppendLine($"var {varName} = ImmutableArray.CreateRange(temp{varName});");
		offset += arrayByteLength;
	}

	private void AppendPrimitiveFieldDeserialization(StringBuilder sb, string varName, int size, string typeName, ref int offset)
	{
		if (typeName == "byte")
			sb.AppendLine($"var {varName} = span[{offset}];");
		else if (typeName == "sbyte")
			sb.AppendLine($"var {varName} = (sbyte)span[{offset}];");
		else
			sb.AppendLine($"var {varName} = BinaryPrimitives.{GetBinaryPrimitivesMethod(typeName)}(span.Slice({offset}, {size}));");
		offset += size;
	}

	private string GenerateArrayDeserializationLoopSimple(string convertedType, int arrayLength, int baseOffset, string varName)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"    var temp{varName} = new {convertedType}[{arrayLength}];");
		sb.AppendLine($"    for (int i_{varName} = 0; i_{varName} < {arrayLength}; i_{varName}++)");
		sb.AppendLine("    {");
		sb.AppendLine($"        int elementOffset = {baseOffset} + i_{varName} * {Utilities.GetDotNetTypeSize(convertedType)};");
		if (convertedType == "byte")
			sb.AppendLine($"        temp{varName}[i_{varName}] = span[elementOffset];");
		else if (convertedType == "sbyte")
			sb.AppendLine($"        temp{varName}[i_{varName}] = (sbyte)span[elementOffset];");
		else
		{
			string bpMethod = GetBinaryPrimitivesMethod(convertedType);
			string cast = convertedType == "char" ? "(char)" : "";
			sb.AppendLine($"        temp{varName}[i_{varName}] = {cast}BinaryPrimitives.{bpMethod}(span.Slice(elementOffset, {Utilities.GetDotNetTypeSize(convertedType)}));");
		}
		sb.AppendLine("    }");
		return sb.ToString();
	}

	private string GetBinaryPrimitivesMethod(string typeName)
	{
		return typeName switch
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
}
