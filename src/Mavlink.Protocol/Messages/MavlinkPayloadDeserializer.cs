#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
using System.Runtime.CompilerServices;
#endif

namespace Mavlink;

/// <summary>
/// Hands a received packet's payload to its message's serializer.
/// </summary>
/// <remarks>
/// Generated deserializers zero-extend short payloads themselves: they are public and
/// must accept raw wire bytes from any caller. On this path the packet's zero-filled
/// inline buffer already contains the restored payload, so the serializer is given a
/// view of at least the length it expects and its own extension copy never runs.
/// MAVLink 1 payloads get the same treatment (zero-extended to the base length), as in
/// the reference C and Python implementations.
/// </remarks>
internal static class MavlinkPayloadDeserializer
{
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
	[SkipLocalsInit]
#endif
	public static T Deserialize<T>(
		in MavlinkReceivedPacket packet,
		IMavlinkMessageInfo<T> info)
		where T : struct, IMavlinkMessage
	{
		bool isV2 = packet.Version == MavlinkPacketVersion.V2;
		int minLength = isV2 ? info.PayloadLengthWithExtensions : info.PayloadLength;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
		ReadOnlySpan<byte> payload = packet.GetPayload(minLength);
#else
		// netstandard2.0 cannot view the packet's buffer as a span (see MavlinkReceivedPacket),
		// so the payload is copied out, zero-extended, to the stack. The copy writes every
		// byte that is read, hence no zero-init.
		Span<byte> scratch = stackalloc byte[MavlinkConstants.MAX_PAYLOAD_LENGTH];
		ReadOnlySpan<byte> payload = scratch.Slice(0, packet.CopyPayloadTo(scratch, minLength));
#endif

		IMavlinkPayloadSerializer<T> serializer = info.PayloadSerializer;
		return isV2
			? serializer.DeserializeV2(payload)
			: serializer.DeserializeV1(payload);
	}

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
	[SkipLocalsInit]
#endif
	public static IMavlinkMessage Deserialize(
		in MavlinkReceivedPacket packet,
		IMavlinkMessageInfo info)
	{
		bool isV2 = packet.Version == MavlinkPacketVersion.V2;
		int minLength = isV2 ? info.PayloadLengthWithExtensions : info.PayloadLength;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
		ReadOnlySpan<byte> payload = packet.GetPayload(minLength);
#else
		Span<byte> scratch = stackalloc byte[MavlinkConstants.MAX_PAYLOAD_LENGTH];
		ReadOnlySpan<byte> payload = scratch.Slice(0, packet.CopyPayloadTo(scratch, minLength));
#endif

		return isV2
			? info.DeserializePayloadV2(payload)
			: info.DeserializePayloadV1(payload);
	}
}
