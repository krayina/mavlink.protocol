using Mavlink.Common;
using Mavlink.Common.Codecs.Metadata;

namespace Mavlink.Dialects;

public sealed class CommonDialect : IMavlinkDialect
{
	public static readonly CommonDialect Instance = new CommonDialect();

	private CommonDialect() { }

	public string Name => "common";

	public IReadOnlyList<IMavlinkDialect> Includes { get; } = new IMavlinkDialect[]
	{
		MinimalDialect.Instance,
	};

	public IReadOnlyList<IMavlinkMessageInfo> Messages { get; } = new IMavlinkMessageInfo[]
	{
		CommandLongMessageInfo.Instance,
		CommandAckMessageInfo.Instance,
		EscInfoMessageInfo.Instance,
	};

	public IMavlinkMessageInfo? GetInfo(uint msgId) => msgId switch
	{
		76 => CommandLongMessageInfo.Instance,
		77 => CommandAckMessageInfo.Instance,
		290 => EscInfoMessageInfo.Instance,
		_ => null
	};

	public IMavlinkMessageInfo? GetInfo(Type type)
	{
		if (type == typeof(CommandLongMavlinkMessage)) return CommandLongMessageInfo.Instance;
		if (type == typeof(CommandAckMavlinkMessage)) return CommandAckMessageInfo.Instance;
		if (type == typeof(EscInfoMavlinkMessage)) return EscInfoMessageInfo.Instance;
		return null;
	}
}
