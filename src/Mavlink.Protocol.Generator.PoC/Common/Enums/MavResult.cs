namespace Mavlink.Common;

public enum MavResult : byte
{
	Accepted = 0,
	TemporarilyRejected = 1,
	Denied = 2,
	Unsupported = 3,
	Failed = 4,
	InProgress = 5,
	Cancelled = 6,
	CommandLongOnly = 7,
	CommandIntOnly = 8,
	CommandUnsupportedMavFrame = 9,
}
