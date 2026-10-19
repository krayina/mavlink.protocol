using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Mavlink;

internal static class MavlinkV2FrameWriter
{
	// ----------------------------------------------------------------------------------------------
	// V2 Header: STX(1) + LEN(1) + INC(1) + CMP(1) + SEQ(1) + SYS(1) + COMP(1) + MSGID(3) = 10 bytes
	// ----------------------------------------------------------------------------------------------

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Write<T>(
		in T message,
		IMavlinkMessageInfo<T> info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer,
		MavlinkSigner? signer) where T : IMavlinkMessage
	{
		// The serializer returns the full payload length, extensions included;
		// truncation is applied below, where the LEN byte is written.
		int payloadLength = info.PayloadSerializer.SerializeV2(
			in message, buffer.Slice(MavlinkConstants.HEADER_V2_LENGTH));

		return AssembleFrame(
			payloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer,
			signer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Write(
		IMavlinkMessage message,
		IMavlinkMessageInfo info,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer,
		MavlinkSigner? signer)
	{
		int payloadLength = info.SerializePayloadV2(
			message, buffer.Slice(MavlinkConstants.HEADER_V2_LENGTH));

		return AssembleFrame(
			payloadLength,
			info.MessageId,
			info.CrcExtra,
			sequence,
			systemId,
			componentId,
			buffer,
			signer);
	}

	/// <summary>
	/// Guards the LEN byte against a serializer returning a length it could not have
	/// written (hand-written IMavlinkPayloadSerializer implementations are possible).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ThrowIfInvalidPayloadLength(int payloadLength, uint msgId)
	{
		if ((uint)payloadLength > MavlinkConstants.MAX_PAYLOAD_LENGTH)
		{
			ThrowInvalidPayloadLength(payloadLength, msgId);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowInvalidPayloadLength(int payloadLength, uint msgId)
		=> throw new InvalidOperationException(
			$"Payload serializer of message {msgId} returned length {payloadLength}; expected 0..255.");

	private static int AssembleFrame(
		int payloadLength,
		uint msgId,
		byte crcExtra,
		byte sequence,
		byte systemId,
		byte componentId,
		Span<byte> buffer,
		MavlinkSigner? signer)
	{
		ThrowIfInvalidPayloadLength(payloadLength, msgId);

		bool isSigned = signer != null;

		// Empty-byte payload truncation (never below one byte).
		int wireLength = PayloadPrimitives.GetTruncatedLength(
			buffer.Slice(MavlinkConstants.HEADER_V2_LENGTH, payloadLength));

		// STX, LEN, INC, CMP, SEQ, SYS, COMP, MSGID(3)
		buffer[0] = MavlinkConstants.MAGIC_V2;      // 0xFD
		buffer[1] = (byte)wireLength;
		buffer[2] = isSigned ? (byte)MavlinkIncompatFlags.Signed : (byte)0;
		buffer[3] = 0;                              // Compatibility Flags
		buffer[4] = sequence;
		buffer[5] = systemId;
		buffer[6] = componentId;

		// Message ID (3 bytes, Little Endian)
		buffer[7] = (byte)msgId;
		buffer[8] = (byte)(msgId >> 8);
		buffer[9] = (byte)(msgId >> 16);

		// CRC includes Header (excluding Magic) + Payload
		int lengthToCrc = MavlinkConstants.HEADER_V2_LENGTH + wireLength;

		// Calculate CRC over [LEN..END_OF_PAYLOAD]
		ushort crc = X25Crc.Calculate(buffer.Slice(1, lengthToCrc - 1));
		crc = X25Crc.Accumulate(crc, crcExtra);

		BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(lengthToCrc), crc);

		// Total: Header + Payload + CRC (2)
		int totalLength = lengthToCrc + MavlinkConstants.CRC_LENGTH;

		// SIGNATURE (13 bytes)
		if (isSigned)
		{
			var packetWithCrc = buffer.Slice(0, totalLength);
			var signatureBlock = buffer.Slice(totalLength, MavlinkConstants.SIGNATURE_LENGTH);
			signer!.SignFrame(packetWithCrc, signatureBlock);
			totalLength += MavlinkConstants.SIGNATURE_LENGTH;
		}
		return totalLength;
	}
}
