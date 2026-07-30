using Mavlink.Dialects;

namespace Mavlink;

internal sealed class MavlinkReceivedPacketCallback : IMavlinkReceivedPacketCallback
{
	private readonly MavlinkMessageHandler _callback;
	private readonly MavlinkPacketFilter? _filter;
	private readonly IMavlinkDialect _dialect;
	private readonly byte? _senderSystemId;
	private readonly byte? _senderComponentId;

	public MavlinkReceivedPacketCallback(
		MavlinkMessageHandler callback,
		MavlinkPacketFilter? filter,
		IMavlinkDialect dialect,
		byte? senderSystemId,
		byte? senderComponentId)
	{
		_callback = callback;
		_filter = filter;
		_dialect = dialect;
		_senderSystemId = senderSystemId;
		_senderComponentId = senderComponentId;
	}

	public void Invoke(in MavlinkReceivedPacket context)
	{
		if (_senderSystemId is byte sys && context.SenderSystemId != sys)
		{
			return;
		}

		if (_senderComponentId is byte comp && context.SenderComponentId != comp)
		{
			return;
		}

		if (_filter != null && !_filter(in context))
		{
			return;
		}

		var info = _dialect.GetInfo(context.MessageId);
		if (info == null)
		{
			return;
		}

		IMavlinkMessage message = MavlinkDeserializer.Deserialize(in context, info);
		_callback(message, in context);
	}
}
