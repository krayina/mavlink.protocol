using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public static class MavlinkXmlMessageParser
{
	public static ImmutableArray<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> ParseMessages(
		IEnumerable<string> xmlContents,
		ImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		var messageDict = new Dictionary<string, (string? Description, List<(string Type, string Name, string? Description)> Fields)>();

		foreach (var xmlContent in xmlContents)
		{
			using var reader = new StringReader(xmlContent);
			var mavlink = (Mavlink)serializer.Deserialize(reader);
			foreach (var m in mavlink.Messages)
			{
				var name = m.Name;
				var fields = m.Field.Select(field =>
				{
					var fieldType = ConvertType(field.Type);
					if (field.Enum is not null && enumTypes.ContainsKey(field.Enum))
					{
						fieldType = enumTypes[field.Enum].TypeName;
					}
					return (fieldType, field.Name, field.Description);
				}).ToList();

				if (messageDict.ContainsKey(name))
				{
					messageDict[name].Fields.AddRange(fields);
				}
				else
				{
					messageDict[name] = (m.Description, fields);
				}
			}
		}

		return messageDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Fields)).ToImmutableArray();
	}

	private static string ConvertType(string xmlType)
	{
		return xmlType switch
		{
			"uint8_t" => "byte",
			"int8_t" => "sbyte",
			"uint16_t" => "ushort",
			"int16_t" => "short",
			"uint32_t" => "uint",
			"int32_t" => "int",
			"uint64_t" => "ulong",
			"int64_t" => "long",
			"float" => "float",
			"double" => "double",
			_ => "object"
		};
	}
}
