namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkMessagesGenerator
{
	public static string GenerateMessageExtensions(IEnumerable<GeneratedMavlinkMessage> messages)
	{
		var messageSizeDictionary = new Dictionary<GeneratedMavlinkMessage, (int MinSize, int MaxSize)>();

		foreach (var message in messages)
		{
			var sizeInfo = CalculateMinAndMaxSize(message);
			messageSizeDictionary[message] = sizeInfo;
		}

		var dictionaryEntriesByType = string.Join(",\n", messages.Select(message =>
		{
			var sizeInfo = messageSizeDictionary[message];
			return $"\t\t\t{{ typeof({message.GeneratedNamespace}.{message.GeneratedName}), ({message.Id}U, \"{message.Name}\", {sizeInfo.MinSize}, {sizeInfo.MaxSize}, payload => {message.GeneratedNamespace}.{message.GeneratedName}.CreateInstance(payload)) }}";
		}));

		var dictionaryEntriesById = string.Join(",\n", messages.Select(message =>
		{
			var sizeInfo = messageSizeDictionary[message];
			return $"\t\t\t{{ {message.Id}U, (typeof({message.GeneratedNamespace}.{message.GeneratedName}), \"{message.Name}\", {sizeInfo.MinSize}, {sizeInfo.MaxSize}, payload => {message.GeneratedNamespace}.{message.GeneratedName}.CreateInstance(payload)) }}";
		}));

		return GetStringCode(dictionaryEntriesByType, dictionaryEntriesById);
	}

	private static (int MinSize, int MaxSize) CalculateMinAndMaxSize(GeneratedMavlinkMessage message)
	{
		int minSize = 0;
		int maxSize = 0;

		foreach (var field in message.GeneratedFields)
		{
			int fieldSize = field.GetFieldSize();
			maxSize += fieldSize;

			if (field.IsRequired)
			{
				minSize += fieldSize;
			}
		}

		return (minSize, maxSize);
	}

	private static string GetStringCode(string dictionaryEntriesByType, string dictionaryEntriesById)
	{
		return
$@"using System;
using System.Collections.Generic;

namespace MavlinkTypes
{{
    public static class MavlinkMessages
    {{
        private static readonly Dictionary<Type, (uint Id, string MavlinkName, int MinSize, int MaxSize, Func<byte[], MavlinkMessage> Creator)> _mavlinkMessagesByType = new()
        {{
{dictionaryEntriesByType}
        }};

        private static readonly Dictionary<uint, (Type Type, string MavlinkName, int MinSize, int MaxSize, Func<byte[], MavlinkMessage> Creator)> _mavlinkMessagesById = new()
        {{
{dictionaryEntriesById}
        }};

        public static uint GetId<T>() where T : MavlinkMessage
        {{
            return _mavlinkMessagesByType[typeof(T)].Id;
        }}

        public static string GetMavlinkName<T>() where T : MavlinkMessage
        {{
            return _mavlinkMessagesByType[typeof(T)].MavlinkName;
        }}

        public static string GetMavlinkName(uint id)
        {{
            return _mavlinkMessagesById[id].MavlinkName;
        }}

        public static Type GetType(uint id)
        {{
            return _mavlinkMessagesById[id].Type;
        }}

        public static MavlinkMessage CreateMessageInstance(uint messageId, byte[] payload)
        {{
            var value = _mavlinkMessagesById[messageId];
            return value.Creator(payload);
        }}

        public static MavlinkMessage CreateMessageInstance(Type messageType, byte[] payload)
        {{
            var value = _mavlinkMessagesByType[messageType];
            return value.Creator(payload);
        }}

        public static bool TryCreateMessageInstance(uint messageId, byte[] payload, out MavlinkMessage message)
        {{
            if (_mavlinkMessagesById.TryGetValue(messageId, out var value))
            {{
                message = value.Creator(payload);
                return true;
            }}
            message = default(MavlinkMessage);
            return false;
        }}

        public static bool TryCreateMessageInstance(Type messageType, byte[] payload, out MavlinkMessage message)
        {{
            if (_mavlinkMessagesByType.TryGetValue(messageType, out var value))
            {{
                message = value.Creator(payload);
                return true;
            }}
            message = default(MavlinkMessage);
            return false;
        }}

        public static bool TryGetId<T>(out uint id) where T : MavlinkMessage
        {{
            var isExists = _mavlinkMessagesByType.TryGetValue(typeof(T), out var value);
            id = isExists ? value.Id : default;
            return isExists;
        }}

        public static bool TryGetMavlinkName<T>(out string mavlinkName) where T : MavlinkMessage
        {{
            var isExists = _mavlinkMessagesByType.TryGetValue(typeof(T), out var value);
            mavlinkName = isExists ? value.MavlinkName : default;
            return isExists;
        }}

        public static bool TryGetMavlinkName(uint id, out string mavlinkName)
        {{
            var isExists = _mavlinkMessagesById.TryGetValue(id, out var value);
            mavlinkName = isExists ? value.MavlinkName : default;
            return isExists;
        }}

        public static bool TryGetType(uint id, out Type type)
        {{
            var isExists = _mavlinkMessagesById.TryGetValue(id, out var value);
            type = isExists ? value.Type : default;
            return isExists;
        }}
    }}
}}";
	}
}
