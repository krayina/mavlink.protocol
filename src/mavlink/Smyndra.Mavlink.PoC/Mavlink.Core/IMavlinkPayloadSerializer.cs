namespace Mavlink;

public interface IMavlinkPayloadSerializer<T> where T : IMavlinkMessage
{
	int Serialize(T message, Span<byte> destination);
	T Deserialize(ReadOnlySpan<byte> payload);
}
