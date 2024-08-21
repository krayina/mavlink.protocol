namespace Shmyndra.Mavlink.V1;

/// <summary>
/// Represents a MAVLink V1 packet.
/// </summary>
public record struct MavlinkV1Packet : MavlinkPacket
{
	/// <summary>
	/// Gets the start byte. Always 0xFE for V1.
	/// </summary>
	public byte StartByte { get; init; }

	/// <summary>
	/// Gets the payload length.
	/// </summary>
	public byte PayloadLength { get; init; }

	/// <summary>
	/// Gets the packet sequence number.
	/// </summary>
	public byte PacketSequence { get; init; }

	/// <summary>
	/// Gets the system identifier.
	/// </summary>
	public byte SystemIdentifier { get; init; }

	/// <summary>
	/// Gets the component identifier.
	/// </summary>
	public byte ComponentIdentifier { get; init; }

	/// <summary>
	/// Gets the message identifier.
	/// </summary>
	public byte MessageIdentifier { get; init; }

	/// <summary>
	/// Gets the payload.
	/// </summary>
	public byte[] Payload { get; init; }

	/// <summary>
	/// Gets the checksum.
	/// </summary>
	public ushort Checksum { get; init; }
}
