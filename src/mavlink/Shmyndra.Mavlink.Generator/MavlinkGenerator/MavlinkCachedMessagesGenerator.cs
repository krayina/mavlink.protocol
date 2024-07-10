namespace Shmyndra.Mavlink.Generator;

internal record MavlinkCachedMessage(string FullName, uint Id, string XmlName);

internal static class MavlinkCachedMessagesGenerator
{
	public static string GenerateMessagesCache(IEnumerable<MavlinkCachedMessage> messages)
	{
		var dictionaryEntriesByType = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ typeof({message.FullName}), ({message.Id}U, \"{message.XmlName}\") }}"));

		var dictionaryEntriesById = string.Join(",\n", messages.Select(message =>
			$"\t\t\t{{ {message.Id}U, (typeof({message.FullName}), \"{message.XmlName}\") }}"));

		var classCode =
$@"using System;
using System.Collections.Generic;

namespace MavlinkTypes
{{
    public static class MavlinkMessages
    {{
        private static readonly Dictionary<Type, (uint Id, string XmlName)> _mavlinkMessagesByType = new()
        {{
{dictionaryEntriesByType}
        }};

        private static readonly Dictionary<uint, (Type Type, string XmlName)> _mavlinkMessagesById = new()
        {{
{dictionaryEntriesById}
        }};

        public static uint GetId<T>() where T : MavlinkMessage
        {{
            return _mavlinkMessagesByType[typeof(T)].Id;
        }}

        public static string GetXmlName<T>() where T : MavlinkMessage
        {{
            return _mavlinkMessagesByType[typeof(T)].XmlName;
        }}

        public static string GetXmlName(uint id)
        {{
            return _mavlinkMessagesById[id].XmlName;
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

        public static bool TryGetXmlName<T>(out string xmlName) where T : MavlinkMessage
        {{
            var isExists = _mavlinkMessagesByType.TryGetValue(typeof(T), out var value);
            xmlName = isExists ? value.XmlName : default;
            return isExists;
        }}

        public static bool TryGetXmlName(uint id, out string xmlName)
        {{
            var isExists = _mavlinkMessagesById.TryGetValue(id, out var value);
            xmlName = isExists ? value.XmlName : default;
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
