namespace Mavlink.Minimal;

//[MavlinkGenerated(dialect: "common", xmlHash: "a1b2c3...")]
public readonly record struct HeartbeatMavlinkMessage : IMavlinkMessage
{
	public MavType Type { get; init; }
	public MavAutopilot Autopilot { get; init; }

	// display="bitmask" + MAV_MODE_FLAG bitmask="true" -> generated enum bitmask.
	// MavModeFlag converts implicitly, so `BaseMode = MavModeFlag.SafetyArmed | ...` still works.
	public MavModeFlagBitmask BaseMode { get; init; }

	public uint CustomMode { get; init; }
	public MavState SystemStatus { get; init; }

	/// <summary>
	/// MAVLink version of the sender (uint8_t_mavlink_version). Read-only for users: the
	/// serializer always writes the dialect's &lt;version&gt;, like the reference implementations.
	/// </summary>
	public byte MavlinkVersion { get; internal init; }
}
