using System.Collections.Concurrent;
using Mavlink.Dialects;
using Mavlink.Protocol;

namespace Mavlink;

internal sealed class MavlinkEventBus
{
	private readonly IMavlinkDialect _dialect;
	private readonly ConcurrentDictionary<uint, IMavlinkHandlerGroup> _typed = new();
	private readonly MavlinkWildcardHandlerGroup _all;
	private long _nextToken;

	internal event Action<Exception>? ErrorReceived;

	public MavlinkEventBus(IMavlinkDialect dialect)
	{
		_dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
		_all = new MavlinkWildcardHandlerGroup(_dialect);
	}

	internal void RaiseError(Exception ex)
	{
		try
		{
			ErrorReceived?.Invoke(ex);
		}
		catch
		{
			// Prevent cascading failures
		}
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

		var group = _typed.GetOrAdd(
			info.MessageId,
			_ => new MavlinkTypedHandlerGroup<T>(info));

		var typedGroup = group as MavlinkTypedHandlerGroup<T>
			?? throw new InvalidOperationException(
				$"Message id {info.MessageId} is already bound to a different CLR type " +
				$"({group.GetType().Name}); cannot subscribe as {typeof(T).Name}.");

		long token = Interlocked.Increment(ref _nextToken);
		typedGroup.Add(token, callback, filter, senderSystemId, senderComponentId);
		return new MavlinkSubscription(typedGroup, token);
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

		long token = Interlocked.Increment(ref _nextToken);
		_all.Add(token, callback, filter, senderSystemId, senderComponentId);
		return new MavlinkSubscription(_all, token);
	}

	public void Publish(in MavlinkReceivedPacket context)
	{
		if (_typed.TryGetValue(context.MessageId, out var group))
		{
			try
			{
				group.Invoke(in context, this);
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}

		try
		{
			_all.Invoke(in context, this);
		}
		catch (Exception ex)
		{
			RaiseError(ex);
		}
	}
}
