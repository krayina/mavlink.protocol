namespace Shmyndra.Mavlink.V1;

public static class MavlinkV1PacketConverter
{
	public static MavlinkV1Packet FromByteArray(byte[] data)
	{
		if (data == null || data.Length < 8) // The minimum packet length for MAVLink V1 is 8 bytes.
		{
			throw new ArgumentException("Invalid data length for MAVLink V1 packet.");
		}

		using (var stream = new MemoryStream(data))
		using (var reader = new BinaryReader(stream))
		{
			return new MavlinkV1Packet
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
}
