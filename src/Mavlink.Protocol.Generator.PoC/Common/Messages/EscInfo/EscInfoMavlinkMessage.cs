namespace Mavlink.Common;

/// <summary>
/// ESC_INFO (#290): ESC information for lower rate streaming. MAVLink 2 only (id &gt; 255).
/// </summary>
//[MavlinkGenerated(dialect: "common", xmlHash: "a1b2c3...")]
public readonly record struct EscInfoMavlinkMessage : IMavlinkMessage
{
	/// <summary>
	/// Index of the first ESC in this message (instance="true"). minValue = 0, maxValue = 60, increment = 4.
	/// </summary>
	public byte Index { get; init; }

	/// <summary>
	/// Timestamp, us (UNIX epoch or time since boot).
	/// </summary>
	public ulong TimeUsec { get; init; }

	public ushort Counter { get; init; }

	/// <summary>
	/// Total number of ESCs in all messages of this type. Elements whose ESC index is
	/// higher than this hold no data; the schema cannot express that, so it is not decoded.
	/// </summary>
	public byte Count { get; init; }

	public EscConnectionType ConnectionType { get; init; }

	/// <summary>
	/// Online/offline status of each ESC: display="bitmask" without an enum.
	/// </summary>
	public ByteBitmask Info { get; init; }

	/// <summary>
	/// uint16_t[4], enum ESC_FAILURE_FLAGS (bitmask="true"): one generated bitmask per ESC.
	/// </summary>
	public FixedArray4<EscFailureFlagsBitmask> FailureFlags { get; init; }

	public FixedArray4<uint> ErrorCount { get; init; }

	/// <summary>
	/// Temperature of each ESC, cdegC. invalid="[INT16_MAX]" marks elements individually,
	/// so each element is an <see cref="Invalidatable{T}"/>.
	/// </summary>
	public FixedArray4<Invalidatable<short>> Temperature { get; init; }
}
