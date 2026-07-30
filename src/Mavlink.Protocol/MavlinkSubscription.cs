namespace Mavlink;

internal sealed class MavlinkSubscription : IDisposable
{
	private MavlinkReceivedPacketCallbackRegistry? _registry;
	private readonly IMavlinkReceivedPacketCallback _handler;

	public MavlinkSubscription(
		MavlinkReceivedPacketCallbackRegistry registry,
		IMavlinkReceivedPacketCallback handler)
	{
		_registry = registry;
		_handler = handler;
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _registry, null)?.Remove(_handler);
	}
}
