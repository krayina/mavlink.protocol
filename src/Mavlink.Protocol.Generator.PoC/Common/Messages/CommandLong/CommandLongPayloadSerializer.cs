namespace Mavlink.Common.Codecs.Payload;

public sealed class CommandLongPayloadSerializer : IMavlinkPayloadSerializer<CommandLongMavlinkMessage>
{
	public int SerializeV1(in CommandLongMavlinkMessage message, Span<byte> destination)
	{
		// serialization...
		return 33;
	}

	public int SerializeV2(in CommandLongMavlinkMessage message, Span<byte> destination)
	{
		return SerializeV1(in message, destination);
	}

	public CommandLongMavlinkMessage DeserializeV1(ReadOnlySpan<byte> payload)
	{
		// deserialization...
		return new CommandLongMavlinkMessage();
	}

	public CommandLongMavlinkMessage DeserializeV2(ReadOnlySpan<byte> payload)
	{
		// deserialization...
		return new CommandLongMavlinkMessage();
	}
}
