namespace Shmyndra.Mavlink.V2;

/// <summary>
/// Represents a MAVLink V2 packet.
/// </summary>
public record struct MavlinkV2Packet
{
	/// <summary>
	/// Gets the start byte. Always 0xFD for V2.
	/// </summary>
	public byte StartByte { get; init; }

	/// <summary>
	/// Gets the payload length.
	/// </summary>
	public byte PayloadLength { get; init; }

	/// <summary>
	/// Gets the incompatibility flags.
	/// </summary>
	public byte IncompatibilityFlags { get; init; }

	/// <summary>
	/// Gets the compatibility flags.
	/// </summary>
	public byte CompatibilityFlags { get; init; }

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
	public uint MessageIdentifier { get; init; } // This is a 24-bit field, hence using uint for simplicity

	/// <summary>
	/// Gets the payload.
	/// </summary>
	public byte[] Payload { get; init; }

	/// <summary>
	/// Gets the checksum.
	/// </summary>
	public ushort Checksum { get; init; }

	/// <summary>
	/// Gets the signature.
	/// </summary>
	public byte[]? Signature { get; init; }
}
