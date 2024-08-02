namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkMessagesGenerator
{
	public static string GenerateMessagesCache(IEnumerable<GeneratedMavlinkMessage> messages)
	{
		var dictionaryEntriesByType = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ typeof({message.GeneratedNamespace}.{message.GeneratedName}), ({message.Id}U, \"{message.Name}\") }}"));

		var dictionaryEntriesById = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ {message.Id}U, (typeof({message.GeneratedNamespace}.{message.GeneratedName}), \"{message.Name}\") }}"));

		var classCode =
$@"using System;
using System.Collections.Generic;

namespace MavlinkTypes
{{
    public static class MavlinkMessages
    {{
        private static readonly Dictionary<Type, (uint Id, string MavlinkName)> _mavlinkMessagesByType = new()
        {{
{dictionaryEntriesByType}
        }};

        private static readonly Dictionary<uint, (Type Type, string MavlinkName)> _mavlinkMessagesById = new()
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
