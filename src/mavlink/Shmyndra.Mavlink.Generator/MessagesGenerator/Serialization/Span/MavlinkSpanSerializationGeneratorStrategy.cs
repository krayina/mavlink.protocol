using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkSpanSerializationGeneratorStrategy : IMavlinkSerializationGeneratorStrategy
{
	private readonly IMavlinkFieldSerializationStrategy _bitmaskStrategy;
	private readonly IMavlinkFieldSerializationStrategy _nonBitmaskStrategy;

	public MavlinkSpanSerializationGeneratorStrategy()
	{
#if USE_OBJECTIVE_BITMASK_SERIALIZATION_AND_DESERIALIZATION
        _bitmaskStrategy = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();
#else
		_bitmaskStrategy = new BitmaskFieldSpanSerializationStrategy();
#endif
		_nonBitmaskStrategy = new NonBitmaskFieldSpanSerializationStrategy();
	}

	public void AppendBufferInitialization(StringBuilder sb, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
		sb.AppendLine("Span<byte> finalSpan = buffer.AsSpan();");
	}

	public void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace)
	{
		if (field.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
			(field.GeneratedType is GeneratedMavlinkMessageFieldEnumType || field.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
		{
			_bitmaskStrategy.SerializeField(sb, field, ref offset, variableName, currentNamespace);
		}
		else
		{
			_nonBitmaskStrategy.SerializeField(sb, field, ref offset, variableName, currentNamespace);
		}
	}

	public void AppendReturnStatement(StringBuilder sb)
	{
		sb.AppendLine("return buffer;");
	}
}
