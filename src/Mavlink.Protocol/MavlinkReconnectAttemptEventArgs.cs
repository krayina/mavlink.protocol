namespace Mavlink;

public readonly struct MavlinkReconnectAttemptEventArgs
{
	public int Attempt { get; init; }
	public Exception? Error { get; init; }
	public TimeSpan? NextDelay { get; init; }
	public bool IsInitialConnect { get; init; }
}
