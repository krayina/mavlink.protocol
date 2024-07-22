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
		var mavlink = DeserializeMavlink(xmlContent);
		var mavlinkData = mavlink.ConvertToMavlinkData();
		return mavlinkData;
	}

	private Mavlink DeserializeMavlink(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		using var reader = new StringReader(xmlContent);
		return (Mavlink)serializer.Deserialize(reader);
	}
}
