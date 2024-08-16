namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkMessagesGenerator
{
	public static string GenerateMessageExtensions(IEnumerable<GeneratedMavlinkMessage> messages)
	{
		var dictionaryEntriesByType = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ typeof({message.GeneratedNamespace}.{message.GeneratedName}), ({message.Id}U, \"{message.Name}\", payload => {message.GeneratedNamespace}.{message.GeneratedName}.CreateInstance(payload)) }}"));

		var dictionaryEntriesById = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ {message.Id}U, (typeof({message.GeneratedNamespace}.{message.GeneratedName}), \"{message.Name}\", payload => {message.GeneratedNamespace}.{message.GeneratedName}.CreateInstance(payload)) }}"));

		var classCode =
$@"using System;
using System.Collections.Generic;

namespace MavlinkTypes
{{
    public static class MavlinkMessages
    {{
        private static readonly Dictionary<Type, (uint Id, string MavlinkName, Func<byte[], MavlinkMessage> Creator)> _mavlinkMessagesByType = new()
        {{
{dictionaryEntriesByType}
        }};

        private static readonly Dictionary<uint, (Type Type, string MavlinkName, Func<byte[], MavlinkMessage> Creator)> _mavlinkMessagesById = new()
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
            return _mavlinkMessagesById[messageId].Creator(payload);
        }}

        public static MavlinkMessage CreateMessageInstance(Type messageType, byte[] payload)
        {{
            return _mavlinkMessagesByType[messageType].Creator(payload);
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
		return classCode;
	}
}
