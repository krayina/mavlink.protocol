namespace Mavlink.Common.PayloadSerialization;

public class HeartbeatPayloadSerializer : IMavlinkPayloadSerializer<HeartbeatMavlinkMessage>
{
	public int Serialize(HeartbeatMavlinkMessage message, Span<byte> destination)
	{
		// serialization...
		return 9;
	}

	public HeartbeatMavlinkMessage Deserialize(ReadOnlySpan<byte> payload)
	{
		// deserialization...
		return new HeartbeatMavlinkMessage();
	}
}
