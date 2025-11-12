using System.Buffers.Binary;

namespace Mavlink;

public static class MavlinkPacketSerializer
{
	public static int Serialize<T>(
		T message,
		IMavlinkMessageInfo<T> info,
		byte sequence,
		// byte systemId, 
		// byte componentId, 
		Span<byte> destination) where T : IMavlinkMessage
	{
		// 1. Серіалізуємо payload напряму в буфер, зсунувши вказівник на розмір хедера
		var payloadSpan = destination.Slice(MavlinkPacket.HEADER_LENGTH);
		int payloadLength = info.PayloadSerializer.Serialize(message, payloadSpan);

		// 2. Заповнюємо хедер
		destination[0] = MavlinkPacket.STX; // Magic byte
		destination[1] = (byte)payloadLength;
		destination[2] = sequence;
		// ... sysid, compid
		// ... msgid (3 байти)

		// 3. Рахуємо CRC
		var crc = CrcX25.Calculate(destination.Slice(1, MavlinkPacket.HEADER_LENGTH - 1 + payloadLength));
		crc = CrcX25.Accumulate(info.CrcExtra, crc);

		// 4. Записуємо CRC
		var crcSpan = destination.Slice(MavlinkPacket.HEADER_LENGTH + payloadLength);
		BinaryPrimitives.WriteUInt16LittleEndian(crcSpan, crc);

		return MavlinkPacket.HEADER_LENGTH + payloadLength + 2; // Повна довжина пакета
	}

	// ... схожі методи для десеріалізації
}
