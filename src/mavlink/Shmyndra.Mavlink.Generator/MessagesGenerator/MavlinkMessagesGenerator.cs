namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkMessagesGenerator
{
	public static string GenerateMessageExtensions(IEnumerable<GeneratedMavlinkMessage> messages)
	{
		var messageValues = new Dictionary<GeneratedMavlinkMessage, (int MinSize, int MaxSize, byte CrcExtra)>();

		foreach (var message in messages)
		{
			var sizeInfo = CalculateMinAndMaxSize(message);
			byte crcExtra = message.GeneratedFields.CalculateCrcExtra(message.Name);
			messageValues[message] = (sizeInfo.MinSize, sizeInfo.MaxSize, crcExtra);
		}

		var dictionaryEntriesByType = string.Join(",\n", messages.Select(message =>
		{
			var sizeInfo = messageValues[message];
			return $"\t\t\t{{ typeof({message.GeneratedNamespace}.{message.GeneratedName}), ({message.Id}U, \"{message.Name}\", {sizeInfo.MinSize}, {sizeInfo.MaxSize}, {sizeInfo.CrcExtra}, payload => {message.GeneratedNamespace}.{message.GeneratedName}.Deserialize(payload)) }}";
		}));

		var dictionaryEntriesById = string.Join(",\n", messages.Select(message =>
		{
			var sizeInfo = messageValues[message];
			return $"\t\t\t{{ {message.Id}U, (typeof({message.GeneratedNamespace}.{message.GeneratedName}), \"{message.Name}\", {sizeInfo.MinSize}, {sizeInfo.MaxSize}, {sizeInfo.CrcExtra}, payload => {message.GeneratedNamespace}.{message.GeneratedName}.Deserialize(payload)) }}";
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
        private static readonly Dictionary<Type, (uint Id, string MavlinkName, int MinSize, int MaxSize, byte CrcExtra, Func<byte[], MavlinkMessage> Deserializer)> _mavlinkMessagesByType = new()
        {{
{dictionaryEntriesByType}
        }};

        private static readonly Dictionary<uint, (Type Type, string MavlinkName, int MinSize, int MaxSize, byte CrcExtra, Func<byte[], MavlinkMessage> Deserializer)> _mavlinkMessagesById = new()
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

        public static byte GetCrcExtra<T>() where T : MavlinkMessage
        {{
            return _mavlinkMessagesByType[typeof(T)].CrcExtra;
        }}

        public static byte GetCrcExtra(uint id)
        {{
            return _mavlinkMessagesById[id].CrcExtra;
        }}

        public static bool TryGetCrcExtra<T>(out byte crcExtra) where T : MavlinkMessage
        {{
            var isExists = _mavlinkMessagesByType.TryGetValue(typeof(T), out var value);
            crcExtra = isExists ? value.CrcExtra : default;
            return isExists;
        }}

        public static bool TryGetCrcExtra(uint id, out byte crcExtra)
        {{
            var isExists = _mavlinkMessagesById.TryGetValue(id, out var value);
            crcExtra = isExists ? value.CrcExtra : default;
            return isExists;
        }}

        public static Type GetType(uint id)
        {{
            return _mavlinkMessagesById[id].Type;
        }}

        public static MavlinkMessage Deserialize(uint messageId, byte[] payload)
        {{
            var value = _mavlinkMessagesById[messageId];
            return value.Deserializer(payload);
        }}

        public static MavlinkMessage Deserialize(Type messageType, byte[] payload)
        {{
            var value = _mavlinkMessagesByType[messageType];
            return value.Deserializer(payload);
        }}

        public static bool TryDeserialize(uint messageId, byte[] payload, out MavlinkMessage message)
        {{
            if (_mavlinkMessagesById.TryGetValue(messageId, out var value))
            {{
                message = value.Deserializer(payload);
                return true;
            }}
            message = default(MavlinkMessage);
            return false;
        }}

        public static bool TryDeserialize(Type messageType, byte[] payload, out MavlinkMessage message)
        {{
            if (_mavlinkMessagesByType.TryGetValue(messageType, out var value))
            {{
                message = value.Deserializer(payload);
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
