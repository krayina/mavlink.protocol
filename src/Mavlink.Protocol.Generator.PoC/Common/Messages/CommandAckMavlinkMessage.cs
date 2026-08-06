namespace Mavlink.Common;

public readonly record struct CommandAckMavlinkMessage : IMavlinkTargetedMessage
{
	public MavCmd Command { get; init; }
	public MavResult Result { get; init; }

	// extension + sentinel(255="unknown"): null=V1, Invalid=V2/255, Valid=V2/real value
	public Invalidatable<byte>? Progress { get; init; }

	// extension без sentinel: null = V1
	public int? ResultParam2 { get; init; }
	public byte? TargetSystem { get; init; }
	public byte? TargetComponent { get; init; }
}
