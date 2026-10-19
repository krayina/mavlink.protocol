namespace Mavlink;

/// <summary>
/// Converts one message DTO to and from its MAVLink payload bytes. One generated,
/// stateless (thread-safe) implementation per message.
/// </summary>
/// <remarks>
/// <para>
/// <b>Serialize</b> writes the logical payload — every byte of it, in wire order — and
/// returns its length: <c>PayloadLength</c> for V1 (base fields only: extension fields
/// do not exist in a MAVLink 1 payload and are ignored), <c>PayloadLengthWithExtensions</c>
/// for V2. Every byte is written unconditionally, so the destination needs no clearing
/// (the frame writer reuses its buffer). An extension property that is <c>null</c>
/// is written as the field's "not provided" value: its <c>invalid</c> sentinel when the
/// definition has one, otherwise zero. Throws <see cref="ArgumentException"/> when the
/// destination is shorter than the payload.
/// </para>
/// <para>
/// Serializers never apply MAVLink 2 empty-byte truncation. It is a framing rule — it
/// defines the LEN header byte — so the frame writer applies it
/// (<see cref="PayloadPrimitives.GetTruncatedLength"/>). An untruncated payload is
/// itself a valid MAVLink 2 payload, so calling a serializer directly is always safe.
/// </para>
/// <para>
/// <b>Deserialize</b> accepts exactly what arrived on the wire and never throws because
/// of its length. A payload shorter than the definition is zero-extended — that undoes
/// MAVLink 2 truncation, and it is also what the reference C and Python implementations
/// do for MAVLink 1. A longer payload is read up to the known fields (a newer dialect
/// may have appended extensions). <c>DeserializeV1</c> sets every extension property to
/// <c>null</c>; <c>DeserializeV2</c> never does.
/// </para>
/// </remarks>
public interface IMavlinkPayloadSerializer<T> where T : IMavlinkMessage
{
	int SerializeV1(in T message, Span<byte> destination);
	int SerializeV2(in T message, Span<byte> destination);

	T DeserializeV1(ReadOnlySpan<byte> payload);
	T DeserializeV2(ReadOnlySpan<byte> payload);
}
