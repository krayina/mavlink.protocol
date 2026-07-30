using Mavlink.Dialects;
using System.Collections.Concurrent;

namespace Mavlink;

internal sealed class MavlinkEventBus
{
	private readonly IMavlinkDialect _dialect;
	private readonly ConcurrentDictionary<uint, MavlinkReceivedPacketCallbackRegistry> _typed = new();
	private readonly MavlinkReceivedPacketCallbackRegistry _all = new();

	internal event Action<Exception>? ErrorReceived;

	public MavlinkEventBus(IMavlinkDialect dialect)
	{
		_dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
	}

	internal void RaiseError(Exception ex)
	{
		try
		{
			ErrorReceived?.Invoke(ex);
		}
		catch { /* prevent cascading failures */ }
	}

	public IDisposable Subscribe<T>(
		MavlinkMessageHandler<T> callback,
		MavlinkPacketFilter? filter = null)
		where T : struct, IMavlinkMessage
	{
		return Subscribe(callback, filter, null, null);
	}

	internal IDisposable Subscribe<T>(
		MavlinkMessageHandler<T> callback,
		MavlinkPacketFilter? filter,
		byte? senderSystemId,
		byte? senderComponentId)
		where T : struct, IMavlinkMessage
	{
		if (callback is null)
		{
			throw new ArgumentNullException(nameof(callback));
		}

		var raw = _dialect.GetInfo(typeof(T))
			?? throw new ArgumentException($"Type {typeof(T).Name} is not registered in dialect.");

		var info = raw as IMavlinkMessageInfo<T>
			?? throw new InvalidOperationException(
				$"Dialect returned {raw.GetType().Name} for {typeof(T).Name}, " +
				$"which does not implement IMavlinkMessageInfo<{typeof(T).Name}>.");

		var handler = new MavlinkReceivedPacketCallback<T>(
			callback, filter, info, senderSystemId, senderComponentId);

		var list = _typed.GetOrAdd(
			info.MessageId,
			static _ => new MavlinkReceivedPacketCallbackRegistry());

		list.Add(handler);
		return new MavlinkSubscription(list, handler);
	}

	public IDisposable SubscribeAll(
		MavlinkMessageHandler callback,
		MavlinkPacketFilter? filter = null)
	{
		return SubscribeAll(callback, filter, null, null);
	}

	internal IDisposable SubscribeAll(
		MavlinkMessageHandler callback,
		MavlinkPacketFilter? filter,
		byte? senderSystemId,
		byte? senderComponentId)
	{
		if (callback is null)
		{
			throw new ArgumentNullException(nameof(callback));
		}

		var handler = new MavlinkReceivedPacketCallback(
			callback, filter, _dialect, senderSystemId, senderComponentId);

		_all.Add(handler);
		return new MavlinkSubscription(_all, handler);
	}

	public void Publish(in MavlinkReceivedPacket context)
	{
		if (_typed.TryGetValue(context.MessageId, out var list))
		{
			InvokeHandlers(list.Snapshot, in context);
		}

		InvokeHandlers(_all.Snapshot, in context);
	}

	private void InvokeHandlers(
		IMavlinkReceivedPacketCallback[] handlers,
		in MavlinkReceivedPacket context)
	{
		for (int i = 0; i < handlers.Length; i++)
		{
			try
			{
				handlers[i].Invoke(in context);
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}
	}
}
