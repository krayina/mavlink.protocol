namespace Mavlink.Ardupilotmega;

public enum MavCmd : ushort
{
	NavWaypoint = (ushort)global::Mavlink.Common.MavCmd.NavWaypoint,
	NavReturnToLaunch = (ushort)global::Mavlink.Common.MavCmd.NavReturnToLaunch,
	NavTakeoff = (ushort)global::Mavlink.Common.MavCmd.NavTakeoff,
	ComponentArmDisarm = (ushort)global::Mavlink.Common.MavCmd.ComponentArmDisarm,
	RequestMessage = (ushort)global::Mavlink.Common.MavCmd.RequestMessage,
	DoParachute = 208
}
