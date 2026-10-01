namespace Mavlink.Common;

//[MavlinkGenerated(dialect: "common", xmlHash: "a1b2c3...")]
public readonly record struct CommandLongMavlinkMessage : IMavlinkTargetRequiredMessage
{
	public byte TargetSystem { get; init; }
	public byte TargetComponent { get; init; }
	public MavCmd Command { get; init; }
	public byte Confirmation { get; init; }
	public Invalidatable<float> Param1 { get; init; }
	public Invalidatable<float> Param2 { get; init; }
	public Invalidatable<float> Param3 { get; init; }
	public Invalidatable<float> Param4 { get; init; }
	public Invalidatable<float> Param5 { get; init; }
	public Invalidatable<float> Param6 { get; init; }
	public Invalidatable<float> Param7 { get; init; }
}
