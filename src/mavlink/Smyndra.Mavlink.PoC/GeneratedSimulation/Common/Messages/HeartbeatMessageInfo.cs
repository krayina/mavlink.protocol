using Mavlink.Common.PayloadSerialization;

namespace Mavlink.Common;

public sealed class HeartbeatMessageInfo : IMavlinkMessageInfo<HeartbeatMavlinkMessage>
{
	public static readonly HeartbeatMessageInfo Instance = new HeartbeatMessageInfo();

	private HeartbeatMessageInfo() { }

	public uint MessageId => 0;
	public byte CrcExtra => 50;
	public string Name => "HEARTBEAT";
	public Type MessageType => typeof(HeartbeatMavlinkMessage);
	public IMavlinkPayloadSerializer<HeartbeatMavlinkMessage> PayloadSerializer { get; } = new HeartbeatPayloadSerializer();

	public int SerializePayload(IMavlinkMessage message, Span<byte> destination)
	{
		return PayloadSerializer.Serialize((HeartbeatMavlinkMessage)message, destination);
	}

	public IMavlinkMessage DeserializePayload(ReadOnlySpan<byte> payload)
	{
		return PayloadSerializer.Deserialize(payload);
	}
}
