using System.Buffers.Binary;
#if NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Mavlink;

internal static class MavlinkV1FrameWriter
{
	// ---------------------------------------------------------------------------
	// V1 Header: STX(1) + LEN(1) + SEQ(1) + SYS(1) + COMP(1) + MSGID(1) = 6 bytes
	// ---------------------------------------------------------------------------

#if NETSTANDARD2_1_OR_GREATER
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public static int Write<T>(
		T message,
		IMavlinkMessageInfo<T> info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer) where T : IMavlinkMessage
	{
		var payloadSpan = buffer.Slice(MavlinkConstants.HEADER_V1_LENGTH);
		info.PayloadSerializer.SerializeV1(message, payloadSpan);

		return AssembleFrame(
			info.PayloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer);
	}

#if NETSTANDARD2_1_OR_GREATER
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public static int Write(
		IMavlinkMessage message,
		IMavlinkMessageInfo info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer)
	{
		var payloadSpan = buffer.Slice(MavlinkConstants.HEADER_V1_LENGTH);
		info.SerializePayloadV1(message, payloadSpan);

		return AssembleFrame(
			info.PayloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer);
	}

	private static int AssembleFrame(
		int payloadLen,
		uint msgId,
		byte crcExtra,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer)
	{
		if (msgId > 255)
		{
			throw new ArgumentOutOfRangeException(nameof(msgId), $"Message ID {msgId} is too large for Mavlink V1");
		}

		// STX, LEN, SEQ, SYS, COMP, MSGID(1)
		buffer[0] = MavlinkConstants.MAGIC_V1;      // 0xFE
		buffer[1] = (byte)payloadLen;               // V1 does NOT trim zeros
		buffer[2] = sequence;
		buffer[3] = systemId;
		buffer[4] = componentId;
		buffer[5] = (byte)msgId;

		// CRC includes Header (excluding Magic) + Payload
		int lengthToCrc = MavlinkConstants.HEADER_V1_LENGTH + payloadLen;

		// Calculate CRC over [LEN..END_OF_PAYLOAD]
		ushort crc = X25Crc.Calculate(buffer.Slice(1, lengthToCrc - 1));
		crc = X25Crc.Accumulate(crc, crcExtra);

		// Write CRC (Little Endian)
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(lengthToCrc), crc);

		// Total: Header + Payload + CRC (2)
		return lengthToCrc + 2;
	}
}
