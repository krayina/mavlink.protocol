using System.ComponentModel;

namespace Mavlink;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMavlinkMessageInfo
{
	uint MessageId { get; }
	byte CrcExtra { get; }
	string Name { get; }
	Type MessageType { get; }

	int PayloadLength { get; }
	int PayloadLengthWithExtensions { get; }
	bool HasExtensions { get; }

	int SerializePayloadV1(IMavlinkMessage message, Span<byte> destination);
	int SerializePayloadV2(IMavlinkMessage message, Span<byte> destination);

	IMavlinkMessage DeserializePayloadV1(ReadOnlySpan<byte> payload);
	IMavlinkMessage DeserializePayloadV2(ReadOnlySpan<byte> payload);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMavlinkMessageInfo<T> : IMavlinkMessageInfo where T : IMavlinkMessage
{
	IMavlinkPayloadSerializer<T> PayloadSerializer { get; }
}
