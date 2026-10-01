namespace Mavlink;

public enum MavlinkPacketParseResult : byte
{
	Success = 0,
	UnknownMessageId = 1,
	CrcMismatch = 2,
	UnknownVersion = 3,
	InvalidFrameLength = 4,
	UnknownMagicByte = 5,
	UnsupportedIncompatFlags = 6
}
