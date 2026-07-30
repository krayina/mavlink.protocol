namespace Mavlink;

internal sealed class MavlinkSubscription : IDisposable
{
	private IMavlinkHandlerGroup? _group;
	private readonly long _token;

	public MavlinkSubscription(IMavlinkHandlerGroup group, long token)
	{
		_group = group;
		_token = token;
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _group, null)?.Remove(_token);
	}
}
