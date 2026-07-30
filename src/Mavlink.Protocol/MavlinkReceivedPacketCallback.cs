using Mavlink.Dialects;

namespace Mavlink;

internal sealed class MavlinkReceivedPacketCallback : IMavlinkReceivedPacketCallback
{
	private readonly MavlinkMessageHandler _callback;
	private readonly MavlinkPacketFilter? _filter;
	private readonly IMavlinkDialect _dialect;

	public MavlinkReceivedPacketCallback(
		MavlinkMessageHandler callback,
		MavlinkPacketFilter? filter,
		IMavlinkDialect dialect)
	{
		_callback = callback;
		_filter = filter;
		_dialect = dialect;
	}

	public void Invoke(in MavlinkReceivedPacket context)
	{
		if (_filter == null || _filter(in context))
		{
			var info = _dialect.GetInfo(context.MessageId);
			if (info != null)
			{
				IMavlinkMessage message = MavlinkDeserializer.Deserialize(in context, info);
				_callback(message, in context);
			}
		}
	}
}
