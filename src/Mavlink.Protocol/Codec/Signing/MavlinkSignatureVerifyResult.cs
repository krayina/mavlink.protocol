namespace Mavlink;

internal enum MavlinkSignatureVerifyResult
{
	Valid,
	BadSignature,
	TimestampReplay,
	NewStreamTimestampOutOfRange,
}
