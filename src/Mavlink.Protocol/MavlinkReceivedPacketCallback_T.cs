namespace Mavlink;

internal sealed class MavlinkReceivedPacketCallback<T> : IMavlinkReceivedPacketCallback
	where T : struct, IMavlinkMessage
{
	private readonly MavlinkMessageHandler<T> _callback;
	private readonly MavlinkPacketFilter? _filter;
	private readonly IMavlinkMessageInfo<T> _messageInfo;
	private readonly byte? _senderSystemId;
	private readonly byte? _senderComponentId;

	public MavlinkReceivedPacketCallback(
		MavlinkMessageHandler<T> callback,
		MavlinkPacketFilter? filter,
		IMavlinkMessageInfo<T> messageInfo,
		byte? senderSystemId = null,
		byte? senderComponentId = null)
	{
		_callback = callback;
		_filter = filter;
		_messageInfo = messageInfo;
		_senderSystemId = senderSystemId;
		_senderComponentId = senderComponentId;
	}

	public void Invoke(in MavlinkReceivedPacket context)
	{
		if (context.MessageId != _messageInfo.MessageId)
		{
			return;
		}

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

		T typedMessage = MavlinkDeserializer.Deserialize(in context, _messageInfo);
		_callback(typedMessage, in context);
	}
}
