namespace Mavlink.Common;

public readonly record struct CommandAckMavlinkMessage : IMavlinkTargetedMessage
{
	public MavCmd Command { get; init; }
	public MavResult Result { get; init; }

	// Extension + sentinel (UINT8_MAX = "unknown"): null = V1, Invalid = V2/255, valid = V2/value.
	public Invalidatable<byte>? Progress { get; init; }

	// Extensions without a sentinel: null = V1.
	public int? ResultParam2 { get; init; }
	public byte? TargetSystem { get; init; }
	public byte? TargetComponent { get; init; }
}
