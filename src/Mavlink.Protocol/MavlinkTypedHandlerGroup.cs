namespace Mavlink;

internal sealed class MavlinkTypedHandlerGroup<T> : IMavlinkHandlerGroup
	where T : struct, IMavlinkMessage
{
	private readonly IMavlinkMessageInfo<T> _info;
	private readonly object _mutationLock = new();
	private volatile Entry[] _handlers = Array.Empty<Entry>();

	public MavlinkTypedHandlerGroup(IMavlinkMessageInfo<T> info)
	{
		_info = info;
	}

	private readonly struct Entry
	{
		public readonly long Token;
		public readonly MavlinkMessageHandler<T> Callback;
		public readonly MavlinkPacketFilter? Filter;
		public readonly byte? SenderSystemId;
		public readonly byte? SenderComponentId;

		public Entry(
			long token,
			MavlinkMessageHandler<T> callback,
			MavlinkPacketFilter? filter,
			byte? senderSystemId,
			byte? senderComponentId)
		{
			Token = token;
			Callback = callback;
			Filter = filter;
			SenderSystemId = senderSystemId;
			SenderComponentId = senderComponentId;
		}
	}

	public void Add(
		long token,
		MavlinkMessageHandler<T> callback,
		MavlinkPacketFilter? filter,
		byte? senderSystemId,
		byte? senderComponentId)
	{
		lock (_mutationLock)
		{
			var current = _handlers;
			var updated = new Entry[current.Length + 1];
			Array.Copy(current, updated, current.Length);
			updated[current.Length] = new Entry(
				token, callback, filter, senderSystemId, senderComponentId);
			_handlers = updated;
		}
	}

	public bool Remove(long token)
	{
		lock (_mutationLock)
		{
			var current = _handlers;

			int index = -1;
			for (int i = 0; i < current.Length; i++)
			{
				if (current[i].Token == token)
				{
					index = i;
					break;
				}
			}

			if (index < 0)
			{
				return false;
			}

			if (current.Length == 1)
			{
				_handlers = Array.Empty<Entry>();
				return true;
			}

			var updated = new Entry[current.Length - 1];
			Array.Copy(current, 0, updated, 0, index);
			Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
			_handlers = updated;
			return true;
		}
	}

	public void Invoke(in MavlinkReceivedPacket packet, MavlinkEventBus bus)
	{
		var handlers = _handlers;

		T message = default;
		bool deserialized = false;

		for (int i = 0; i < handlers.Length; i++)
		{
			ref readonly var h = ref handlers[i];

			if (h.SenderSystemId is byte sys && packet.SenderSystemId != sys)
			{
				continue;
			}

			if (h.SenderComponentId is byte comp && packet.SenderComponentId != comp)
			{
				continue;
			}

			if (h.Filter != null)
			{
				bool pass;
				try
				{
					pass = h.Filter(in packet);
				}
				catch (Exception ex)
				{
					bus.RaiseError(ex);
					continue;
				}

				if (!pass)
				{
					continue;
				}
			}

			if (!deserialized)
			{
				try
				{
					message = MavlinkDeserializer.Deserialize(in packet, _info);
					deserialized = true;
				}
				catch (Exception ex)
				{
					// Payload is unusable — every handler in this group would fail identically.
					bus.RaiseError(ex);
					return;
				}
			}

			try
			{
				h.Callback(message, in packet);
			}
			catch (Exception ex)
			{
				bus.RaiseError(ex);
			}
		}
	}
}
