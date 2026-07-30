namespace Mavlink;

internal sealed class MavlinkReceivedPacketCallback<T> : IMavlinkReceivedPacketCallback
	where T : struct, IMavlinkMessage
{
	private readonly MavlinkMessageHandler<T> _callback;
	private readonly MavlinkPacketFilter? _filter;
	private readonly IMavlinkMessageInfo<T> _messageInfo;

	public MavlinkReceivedPacketCallback(
		MavlinkMessageHandler<T> callback,
		MavlinkPacketFilter? filter,
		IMavlinkMessageInfo<T> messageInfo)
	{
		_callback = callback;
		_filter = filter;
		_messageInfo = messageInfo;
	}

	public void Invoke(in MavlinkReceivedPacket context)
	{
		if (context.MessageId != _messageInfo.MessageId)
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
