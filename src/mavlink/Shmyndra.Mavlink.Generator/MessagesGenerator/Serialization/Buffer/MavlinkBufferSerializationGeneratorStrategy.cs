using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBufferSerializationGeneratorStrategy : IMavlinkSerializationGeneratorStrategy
{
	private readonly IMavlinkFieldSerializationStrategy _bitmaskStrategy;
	private readonly IMavlinkFieldSerializationStrategy _nonBitmaskStrategy;

	public MavlinkBufferSerializationGeneratorStrategy()
	{
#if USE_OBJECTIVE_BITMASK_SERIALIZATION_AND_DESERIALIZATION
        _bitmaskStrategy = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();
#else
		_bitmaskStrategy = new MavlinkBitmaskFieldBufferSerializationStrategy();
#endif
		_nonBitmaskStrategy = new MavlinkNonBitmaskFieldBufferSerializationStrategy();
	}

	public void AppendBufferInitialization(StringBuilder sb, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
	}

	public void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		if (field.Original.Display == MavlinkMessageFieldDisplay.Bitmask
			&& (field.GeneratedType is GeneratedMavlinkMessageFieldEnumType
				|| field.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
		{
			_bitmaskStrategy.SerializeField(sb, field, ref offset);
		}
		else
		{
			_nonBitmaskStrategy.SerializeField(sb, field, ref offset);
		}
	}

	public void AppendReturnStatement(StringBuilder sb)
	{
		sb.AppendLine("return buffer;");
	}
}
