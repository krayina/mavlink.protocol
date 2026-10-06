namespace Mavlink.Transport;

public interface IMavlinkPortProvider
{
	bool CanRecreatePort { get; }

	bool CanWrite { get; }

	ValueTask<IMavlinkPort> CreatePortAsync(CancellationToken ct);
}
