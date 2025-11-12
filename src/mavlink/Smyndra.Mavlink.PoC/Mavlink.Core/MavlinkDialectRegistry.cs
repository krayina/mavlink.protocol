using System.Collections.Concurrent;

namespace Mavlink;

public static class MavlinkDialectRegistry
{
	private static readonly ConcurrentDictionary<uint, IMavlinkMessageInfo> s_idToInfo = new();
	private static readonly ConcurrentDictionary<Type, IMavlinkMessageInfo> s_typeToInfo = new();

	public static void Register(IEnumerable<IMavlinkMessageInfo> messageInfos)
	{
		foreach (var info in messageInfos)
		{
			s_idToInfo.TryAdd(info.MessageId, info);
			s_typeToInfo.TryAdd(info.MessageType, info);
		}
	}

	public static IMavlinkMessageInfo? GetInfo(uint messageId) => s_idToInfo.GetValueOrDefault(messageId);
	public static IMavlinkMessageInfo? GetInfo<T>() where T : IMavlinkMessage => GetInfo(typeof(T));
	public static IMavlinkMessageInfo? GetInfo(Type messageType) => s_typeToInfo.GetValueOrDefault(messageType);
}
