namespace Shmyndra.Mavlink.V2;

public static class MavlinkV2PacketConverter
{
	/// <summary>
	/// Converts byte array data from MavlinkTcpStream to MavlinkV2Packet.
	/// </summary>
	/// <param name="data">Byte array data from MavlinkTcpStream.</param>
	/// <returns>MavlinkV2Packet object.</returns>
	public static MavlinkV2Packet FromByteArray(byte[] data)
	{
		if (data == null || data.Length < 12) // he minimum packet length for MAVLink V2 is 12 bytes
		{
			throw new ArgumentException("Invalid data length for MAVLink V2 packet.");
		}

		uint messageIdentifier = (uint)(data[7] | (data[8] << 8) | (data[9] << 16));
		byte[] payload = data[10..(10 + data[1])];
		ushort checksum = BitConverter.ToUInt16(data, 10 + data[1]);

		byte[]? signature = null;
		if (data[2] == 1) // Signature check
		{
			signature = data[(12 + data[1])..];
		}

		return new MavlinkV2Packet
		{
			StartByte = data[0],
			PayloadLength = data[1],
			IncompatibilityFlags = data[2],
			CompatibilityFlags = data[3],
			PacketSequence = data[4],
			SystemIdentifier = data[5],
			ComponentIdentifier = data[6],
			MessageIdentifier = messageIdentifier,
			Payload = payload,
			Checksum = checksum,
			Signature = signature
		};
	}
}
