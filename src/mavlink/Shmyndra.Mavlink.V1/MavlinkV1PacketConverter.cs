namespace Shmyndra.Mavlink.V1;

public static class MavlinkV1PacketConverter
{


	public static byte[] To()
	{
		var a = new MavlinkV1ReceivedPacket()
		{
			StartByte = 
			PayloadLength
			PacketSequence
			SystemIdentifier
			ComponentIdentifier
			MessageIdentifier
			Payload
			Checksum
		};



		// Create the header
		var header = new byte[6];
		header[0] = 0xFE; // STX for MAVLink 1.0
		header[1] = (byte)payload.Length; // Length of the payload
		header[2] = sequence; // Message sequence number
		header[3] = systemId; // System ID
		header[4] = componentId; // Component ID
		header[5] = messageId; // Message ID

		// Combine the header and payload
		var packet = new byte[header.Length + payload.Length + 2]; // +2 bytes for CRC
		Buffer.BlockCopy(header, 0, packet, 0, header.Length);
		Buffer.BlockCopy(payload, 0, packet, header.Length, payload.Length);

		// Calculate CRC for the message
		ushort crc = X25Crc.Calculate(packet, 1, header.Length + payload.Length, crcExtra);
		packet[packet.Length - 2] = (byte)(crc & 0xFF); // Low byte of the CRC
		packet[packet.Length - 1] = (byte)((crc >> 8) & 0xFF); // High byte of the CRC
		return packet;
	}
}
