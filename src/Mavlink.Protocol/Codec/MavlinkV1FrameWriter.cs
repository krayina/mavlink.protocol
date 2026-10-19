using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Mavlink;

internal static class MavlinkV1FrameWriter
{
	// ---------------------------------------------------------------------------
	// V1 Header: STX(1) + LEN(1) + SEQ(1) + SYS(1) + COMP(1) + MSGID(1) = 6 bytes
	// ---------------------------------------------------------------------------

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Write<T>(
		in T message,
		IMavlinkMessageInfo<T> info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer) where T : IMavlinkMessage
	{
		ThrowIfNotV1Addressable(info.MessageId);

		int payloadLength = info.PayloadSerializer.SerializeV1(
			in message, buffer.Slice(MavlinkConstants.HEADER_V1_LENGTH));

		return AssembleFrame(
			payloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Write(
		IMavlinkMessage message,
		IMavlinkMessageInfo info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer)
	{
		ThrowIfNotV1Addressable(info.MessageId);

		int payloadLength = info.SerializePayloadV1(
			message, buffer.Slice(MavlinkConstants.HEADER_V1_LENGTH));

		return AssembleFrame(
			payloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer);
	}

	// Checked before serializing: the header has a single MSGID byte, so ids above 255
	// (MAVLink 2-only messages such as ESC_INFO) cannot be framed as V1 at all.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfNotV1Addressable(uint msgId)
	{
		if (msgId > byte.MaxValue)
		{
			ThrowMessageIdTooLarge(msgId);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowMessageIdTooLarge(uint msgId)
		=> throw new ArgumentOutOfRangeException(
			nameof(msgId), msgId, $"Message ID {msgId} is too large for MAVLink 1 (max 255); send it as MAVLink 2.");

	private static int AssembleFrame(
		int payloadLength,
		uint msgId,
		byte crcExtra,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer)
	{
		MavlinkV2FrameWriter.ThrowIfInvalidPayloadLength(payloadLength, msgId);

		// STX, LEN, SEQ, SYS, COMP, MSGID(1)
		buffer[0] = MavlinkConstants.MAGIC_V1;      // 0xFE
		buffer[1] = (byte)payloadLength;            // MAVLink 1 never truncates
		buffer[2] = sequence;
		buffer[3] = systemId;
		buffer[4] = componentId;
		buffer[5] = (byte)msgId;

		// CRC includes Header (excluding Magic) + Payload
		int lengthToCrc = MavlinkConstants.HEADER_V1_LENGTH + payloadLength;

		// Calculate CRC over [LEN..END_OF_PAYLOAD]
		ushort crc = X25Crc.Calculate(buffer.Slice(1, lengthToCrc - 1));
		crc = X25Crc.Accumulate(crc, crcExtra);

		// Write CRC (Little Endian)
		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(lengthToCrc), crc);

		// Total: Header + Payload + CRC (2)
		return lengthToCrc + MavlinkConstants.CRC_LENGTH;
	}
}
