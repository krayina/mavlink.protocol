using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Implements a buffer-based deserialization strategy using BitConverter and Buffer.
/// </summary>
public class MavlinkBufferDeserializationGeneratorStrategy : IMavlinkDeserializationGeneratorStrategy
{
	private readonly IMavlinkFieldDeserializationStrategy _bitmaskStrategy;
	private readonly IMavlinkFieldDeserializationStrategy _nonBitmaskStrategy;

	public MavlinkBufferDeserializationGeneratorStrategy(
		IMavlinkFieldDeserializationStrategy bitmaskStrategy,
		IMavlinkFieldDeserializationStrategy nonBitmaskStrategy)
	{
		_bitmaskStrategy = bitmaskStrategy;
		_nonBitmaskStrategy = nonBitmaskStrategy;
	}

	public void AppendBufferInitialization(StringBuilder sb, string messageName, int requiredSize, string payloadParameterName)
	{
		sb.AppendLine($@"
if ({payloadParameterName}.Length == 0)
{{
    return new {messageName}();
}}
else if ({payloadParameterName}.Length < {requiredSize})
{{
    var paddedPayload = new byte[{requiredSize}];
    Array.Copy({payloadParameterName}, paddedPayload, {payloadParameterName}.Length);
    {payloadParameterName} = paddedPayload;
}}
");
	}

	public string AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName)
	{
		if (field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			(field.GeneratedType is GeneratedMavlinkMessageFieldEnumType || field.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
		{
			return _bitmaskStrategy.DeserializeField(sb, field, ref offset, currentNamespace, payloadParameterName);
		}
		return _nonBitmaskStrategy.DeserializeField(sb, field, ref offset, currentNamespace, payloadParameterName);
	}

	public void AppendReturnStatement(StringBuilder sb, string messageName, IDictionary<GeneratedMavlinkMessageField, string> fields)
	{
		var formattedAssignments = string.Join(",\n    ", fields.Select(kvp =>
			$"{Utilities.EscapeReservedKeyword(kvp.Key.GeneratedName)} = {kvp.Value}"));

		sb.AppendLine($@"
return new {messageName}
{{
    {formattedAssignments}
}};");
	}
}
