using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public static class MavlinkXmlEnumParser
{
	public static ImmutableArray<(string Name, string? Description, List<(string Name, string Value, string? Description)> Entries)> ParseEnums(IEnumerable<string> xmlContents)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		var enumDict = new Dictionary<string, (string? Description, List<(string Name, string Value, string? Description)> Entries)>();

		foreach (var xmlContent in xmlContents)
		{
			using var reader = new StringReader(xmlContent);
			var mavlink = (Mavlink)serializer.Deserialize(reader);
			foreach (var e in mavlink.Enums)
			{
				var entries = e.Entry.Select(entry => (entry.Name, entry.Value, entry.Description)).ToList();

				if (enumDict.ContainsKey(e.Name))
				{
					enumDict[e.Name].Entries.AddRange(entries);
				}
				else
				{
					enumDict[e.Name] = (e.Description, entries);
				}
			}
		}

		return enumDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Entries)).ToImmutableArray();
	}
}
