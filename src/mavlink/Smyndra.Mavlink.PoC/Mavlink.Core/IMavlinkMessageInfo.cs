using System.ComponentModel;

namespace Mavlink;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMavlinkMessageInfo
{
	uint MessageId { get; }
	byte CrcExtra { get; }
	string Name { get; }
	Type MessageType { get; }

	int SerializePayload(IMavlinkMessage message, Span<byte> destination);
	IMavlinkMessage DeserializePayload(ReadOnlySpan<byte> payload);
}

public interface IMavlinkMessageInfo<T> : IMavlinkMessageInfo where T : IMavlinkMessage
{
	IMavlinkPayloadSerializer<T> PayloadSerializer { get; }
}
