namespace Mavlink.Ardupilotmega;

public enum MavCmd : ushort
{
	NavWaypoint = (ushort)Common.MavCmd.NavWaypoint,
	NavReturnToLaunch = (ushort)Common.MavCmd.NavReturnToLaunch,
	NavTakeoff = (ushort)Common.MavCmd.NavTakeoff,
	ComponentArmDisarm = (ushort)Common.MavCmd.ComponentArmDisarm,
	RequestMessage = (ushort)Common.MavCmd.RequestMessage,
	DoParachute = 208
}
