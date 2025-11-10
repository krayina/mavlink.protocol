namespace Shmyndra.Mavlink.V1;

/// <summary>
/// Represents a MAVLink V1 packet that is intended for serialization and transmission.
/// This type is used to construct a MAVLink packet with the necessary headers, payload, 
/// and to calculate the CRC checksum before sending the packet over the communication channel.
/// </summary>
public readonly record struct MavlinkV1SendPacket
{
	/// <summary>
	/// Gets or sets the packet sequence number, used to identify the order of packets.
	/// </summary>
	public byte PacketSequence { get; init; }

	/// <summary>
	/// Gets or sets the system identifier, used to identify the system originating the message.
	/// </summary>
	public byte SystemIdentifier { get; init; }

	/// <summary>
	/// Gets or sets the component identifier, used to identify the component originating the message.
	/// </summary>
	public byte ComponentIdentifier { get; init; }

	/// <summary>
	/// Gets or sets the message identifier, used to identify the type of message being sent.
	/// </summary>
	public byte MessageIdentifier { get; init; }

	/// <summary>
	/// Gets or sets the payload of the MAVLink packet, containing the data to be transmitted.
	/// </summary>
	public byte[] Payload { get; init; }

	/// <summary>
	/// Gets or sets the extra CRC byte specific to the message type, used in CRC calculation.
	/// </summary>
	public byte CrcExtra { get; init; }

	/// <summary>
	/// Serializes the current MAVLink V1 packet into a byte array, including the header,
	/// payload, and CRC checksum. The CRC checksum is calculated based on the packet's contents
	/// and the provided extra CRC byte specific to the message type.
	/// </summary>
	/// <param name="crcExtra">The extra CRC byte used to calculate the CRC checksum for this packet.</param>
	/// <returns>A byte array representing the serialized MAVLink V1 packet, ready for transmission.</returns>
	public byte[] Serialize()
	{
		var payloadLength = (byte)(this.Payload?.Length ?? 0);

		// Create the header
		var header = new byte[6];
		header[0] = 0xFE; // Fixed start byte for MAVLink V1
		header[1] = payloadLength;
		header[2] = this.PacketSequence;
		header[3] = this.SystemIdentifier;
		header[4] = this.ComponentIdentifier;
		header[5] = this.MessageIdentifier;

		// Combine the header and payload
		var packet = new byte[header.Length + payloadLength + 2]; // +2 bytes for CRC
		Buffer.BlockCopy(header, 0, packet, 0, header.Length);
		Buffer.BlockCopy(this.Payload, 0, packet, header.Length, payloadLength);

		// Calculate CRC for the packet
		ushort crc = X25Crc.Calculate(packet, 1, header.Length + payloadLength, this.CrcExtra);
		packet[packet.Length - 2] = (byte)(crc & 0xFF); // Low byte of the CRC
		packet[packet.Length - 1] = (byte)((crc >> 8) & 0xFF); // High byte of the CRC

		return packet;
	}
}
