using System.Xml.Serialization;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkParser
{
	MavlinkData Parse(string content);
}

public class MavlinkXmlParser : IMavlinkParser
{
	public MavlinkData Parse(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		using var reader = new StringReader(xmlContent);
		var mavlink = (Mavlink)serializer.Deserialize(reader);

		return mavlink.ConvertToMavlinkData(fieldName => true); // Assuming all fields are required, change logic if necessary
	}
}
