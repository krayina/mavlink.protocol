using Mavlink.Dialects;

namespace Mavlink.Protocol;

internal sealed class MavlinkWildcardHandlerGroup : IMavlinkHandlerGroup
{
	private readonly IMavlinkDialect _dialect;
	private readonly object _mutationLock = new();
	private volatile Entry[] _handlers = Array.Empty<Entry>();

	public MavlinkWildcardHandlerGroup(IMavlinkDialect dialect)
	{
		_dialect = dialect;
	}

	private readonly struct Entry
	{
		public readonly long Token;
		public readonly MavlinkMessageHandler Callback;
		public readonly MavlinkPacketFilter? Filter;
		public readonly byte? SenderSystemId;
		public readonly byte? SenderComponentId;

		public Entry(
			long token,
			MavlinkMessageHandler callback,
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
		MavlinkMessageHandler callback,
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
		if (handlers.Length == 0)
		{
			return;
		}

		IMavlinkMessage? message = null;

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

			if (message == null)
			{
				var info = _dialect.GetInfo(packet.MessageId);
				if (info == null)
				{
					// unknown id — no handler in this group can be served
					return;
				}

				try
				{
					message = MavlinkDeserializer.Deserialize(in packet, info);
				}
				catch (Exception ex)
				{
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
