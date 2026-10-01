namespace Mavlink.Routing;

public enum MavlinkSystemState : byte
{
	Unknown = 0,
	Alive = 1,
	Silent = 2,
}

public readonly record struct MavlinkSystemStateChange
(
	MavlinkSystemState OldState,
	MavlinkSystemState NewState
);
