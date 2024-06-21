using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public class MavlinkData
{
	public List<(string Name, string? Description, List<(string Name, string Value, string? Description)> Entries)> Enums { get; set; } = new();
	public List<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> Messages { get; set; } = new();
}

public static class MavlinkXmlParser
{
	public static MavlinkData Parse(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		var mavlinkData = new MavlinkData();

		using var reader = new StringReader(xmlContent);
		var mavlink = (Mavlink)serializer.Deserialize(reader);

		foreach (var e in mavlink.Enums)
		{
			var entries = e.Entry.Select(entry => (entry.Name, entry.Value, entry.Description)).ToList();
			mavlinkData.Enums.Add((e.Name, e.Description, entries));
		}

		foreach (var m in mavlink.Messages)
		{
			var fields = m.Field.Select(field =>
			{
				var fieldType = ConvertType(field.Type);
				if (field.Enum is not null)
				{
					fieldType = field.Enum; // We will map enum types later
				}
				return (fieldType, field.Name, field.Description);
			}).ToList();

			mavlinkData.Messages.Add((m.Name, m.Description, fields));
		}

		return mavlinkData;
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
