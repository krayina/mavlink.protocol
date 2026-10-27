using Mavlink.Minimal;
using Mavlink.Minimal.Codecs.Metadata;

namespace Mavlink.Dialects;

public sealed class MinimalDialect : IMavlinkDialect
{
	public static readonly MinimalDialect Instance = new MinimalDialect();

	private MinimalDialect() { }

	public string Name => "minimal";

	public IReadOnlyList<IMavlinkDialect> Includes => Array.Empty<IMavlinkDialect>();

	public IReadOnlyList<IMavlinkMessageInfo> Messages { get; } = new IMavlinkMessageInfo[]
	{
		HeartbeatMessageInfo.Instance,
	};

	public IMavlinkMessageInfo? GetInfo(uint msgId) => msgId switch
	{
		0 => HeartbeatMessageInfo.Instance,
		_ => null
	};

	public IMavlinkMessageInfo? GetInfo(Type type)
	{
		if (type == typeof(HeartbeatMavlinkMessage)) return HeartbeatMessageInfo.Instance;
		return null;
	}
}
