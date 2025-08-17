using System.Text;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkBufferSerializationGeneratorStrategy : IMavlinkSerializationGeneratorStrategy
{
	private readonly IMavlinkFieldSerializationStrategy _bitmaskStrategy;
	private readonly IMavlinkFieldSerializationStrategy _nonBitmaskStrategy;

	public MavlinkBufferSerializationGeneratorStrategy(
		IMavlinkFieldSerializationStrategy bitmaskStrategy,
		IMavlinkFieldSerializationStrategy nonBitmaskStrategy)
	{
		_bitmaskStrategy = bitmaskStrategy;
		_nonBitmaskStrategy = nonBitmaskStrategy;
	}

	public void AppendBufferInitialization(StringBuilder sb, int requiredSize)
	{
		sb.AppendLine($"var buffer = new byte[{requiredSize}];");
	}

	public void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset)
	{
		bool isBitmaskEligible = field.GeneratedType is GeneratedMavlinkMessageFieldEnumType ||
								 field.GeneratedType is GeneratedMavlinkMessageFieldArrayType { ElementType: GeneratedMavlinkMessageFieldEnumType };

		if (field.Original.Display == MavlinkMessageFieldDisplay.Bitmask && isBitmaskEligible)
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
