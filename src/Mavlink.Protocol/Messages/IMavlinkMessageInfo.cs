using System.ComponentModel;

namespace Mavlink;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMavlinkMessageInfo
{
	uint MessageId { get; }
	byte CrcExtra { get; }
	string Name { get; }
	Type MessageType { get; }

	/// <summary>
	/// Length of the base fields: the MAVLink 1 payload, and the minimum length of a MAVLink 2 one.
	/// </summary>
	int PayloadLength { get; }

	/// <summary>
	/// Length including extension fields: the untruncated MAVLink 2 payload.
	/// Equals <see cref="PayloadLength"/> when the message has no extensions.
	/// </summary>
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
