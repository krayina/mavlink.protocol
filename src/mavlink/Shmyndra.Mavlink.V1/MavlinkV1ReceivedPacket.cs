namespace Shmyndra.Mavlink.V1;

/// <summary>
/// Represents a MAVLink V1 packet that has been received and deserialized.
/// This type is used to hold the data extracted from an incoming MAVLink packet,
/// including the headers, payload, and the CRC checksum.
/// </summary>
public record struct MavlinkV1ReceivedPacket
{
	/// <summary>
	/// Gets the start byte of the MAVLink packet. Always 0xFE for MAVLink V1.0.
	/// </summary>
	public byte StartByte { get; init; }

	/// <summary>
	/// Gets the length of the payload in bytes.
	/// </summary>
	public byte PayloadLength { get; init; }

	/// <summary>
	/// Gets the packet sequence number, which identifies the order of the received packet.
	/// </summary>
	public byte PacketSequence { get; init; }

	/// <summary>
	/// Gets the system identifier, which identifies the system that originated the message.
	/// </summary>
	public byte SystemIdentifier { get; init; }

	/// <summary>
	/// Gets the component identifier, which identifies the component that originated the message.
	/// </summary>
	public byte ComponentIdentifier { get; init; }

	/// <summary>
	/// Gets the message identifier, which identifies the type of message that was received.
	/// </summary>
	public byte MessageIdentifier { get; init; }

	/// <summary>
	/// Gets the payload of the MAVLink packet, containing the data that was transmitted.
	/// </summary>
	public byte[] Payload { get; init; }

	/// <summary>
	/// Gets the CRC checksum that was included in the received MAVLink packet.
	/// This checksum is used to verify the integrity of the packet.
	/// </summary>
	public ushort Checksum { get; init; }

	/// <summary>
	/// Deserializes the given byte array into a <see cref="MavlinkV1ReceivedPacket"/>.
	/// This method extracts the MAVLink V1 packet fields from the byte array,
	/// including the start byte, payload length, packet sequence, system identifier,
	/// component identifier, message identifier, payload, and checksum.
	/// </summary>
	/// <param name="data">The byte array containing the serialized MAVLink V1 packet data.</param>
	/// <returns>A <see cref="MavlinkV1ReceivedPacket"/> object populated with the deserialized data.</returns>
	/// <exception cref="ArgumentException">Thrown if the data is null or the length is less than the minimum required for a MAVLink V1 packet.</exception>
	public static MavlinkV1ReceivedPacket Deserialize(byte[] data)
	{
		if (data == null || data.Length < 8) // The minimum packet length for MAVLink V1 is 8 bytes.
		{
			throw new ArgumentException("Invalid data length for MAVLink V1 packet.");
		}

		return new MavlinkV1ReceivedPacket
		{
			StartByte = data[0],
			PayloadLength = data[1],
			PacketSequence = data[2],
			SystemIdentifier = data[3],
			ComponentIdentifier = data[4],
			MessageIdentifier = data[5],
			Payload = data[6..(6 + data[1])],
			Checksum = BitConverter.ToUInt16(data, 6 + data[1])
		};
	}
}
