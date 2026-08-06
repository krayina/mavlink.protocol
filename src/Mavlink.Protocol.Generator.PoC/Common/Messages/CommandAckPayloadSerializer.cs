namespace Mavlink.Common.Codecs.Payload;

public class CommandAckPayloadSerializer : IMavlinkPayloadSerializer<CommandAckMavlinkMessage>
{
	public CommandAckMavlinkMessage DeserializeV1(ReadOnlySpan<byte> payload)
	{
		throw new NotImplementedException();
	}

	public CommandAckMavlinkMessage DeserializeV2(ReadOnlySpan<byte> payload)
	{
		throw new NotImplementedException();
	}

	public int SerializeV1(CommandAckMavlinkMessage message, Span<byte> destination)
	{
		throw new NotImplementedException();
	}

	public int SerializeV2(CommandAckMavlinkMessage message, Span<byte> destination)
	{
		throw new NotImplementedException();
	}
}
