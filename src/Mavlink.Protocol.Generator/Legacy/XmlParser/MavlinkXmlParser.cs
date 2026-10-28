using System.Xml.Serialization;

namespace Mavlink.Protocol.Generator;

public interface IMavlinkParser
{
	MavlinkData Parse(string content);
}

public class MavlinkXmlParser : IMavlinkParser
{
	public MavlinkData Parse(string xmlContent)
	{
		var mavlink = DeserializeMavlink(xmlContent);
		var mavlinkData = mavlink.ConvertToMavlinkData(SortEnumsByDependencies);
		return mavlinkData;
	}

	private Mavlink DeserializeMavlink(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		using var reader = new StringReader(xmlContent);
		return (Mavlink)serializer.Deserialize(reader);
	}

	private IEnumerable<Enum> SortEnumsByDependencies(IEnumerable<Enum> enums)
	{
		var enumDict = enums.ToDictionary(e => e.Name);
		var sortedEnums = new List<Enum>();
		var visited = new HashSet<string>();

		void Visit(string enumName)
		{
			if (visited.Contains(enumName))
			{
				return;
			}

			visited.Add(enumName);

			if (enumDict.TryGetValue(enumName, out var enumValue))
			{
				foreach (var entry in enumValue.Entry)
				{
					foreach (var param in entry.Param)
					{
						if (!string.IsNullOrEmpty(param.Enum))
						{
							Visit(param.Enum!);
						}
					}
				}

				sortedEnums.Add(enumValue);
			}
		}

		foreach (var enumName in enumDict.Keys)
		{
			Visit(enumName);
		}

		return sortedEnums;
	}
}
