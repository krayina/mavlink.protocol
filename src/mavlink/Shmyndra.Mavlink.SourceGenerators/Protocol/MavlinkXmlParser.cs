using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public record MavlinkData(
	ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> Enums,
	ImmutableArray<(string Name, string? Description, ImmutableList<(string Type, string Name, string? Description)> Fields)> Messages,
	byte? Version,
	byte? Dialect);

public static class MavlinkXmlParser
{
	public static MavlinkData Parse(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		using var reader = new StringReader(xmlContent);
		var mavlink = (Mavlink)serializer.Deserialize(reader);

		var enums = ParseEnums(mavlink);
		var messages = ParseMessages(mavlink);

		return new MavlinkData(
			enums,
			messages,
			mavlink.VersionSpecified ? (byte?)mavlink.Version : null,
			mavlink.DialectSpecified ? (byte?)mavlink.Dialect : null);
	}

	private static ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> ParseEnums(Mavlink mavlink)
	{
		return mavlink.Enums.Select(e => (
			e.Name,
			e.Description,
			e.Entry.Select(entry => (entry.Name, entry.Value, (string?)entry.Description)).ToImmutableList()
		)).ToImmutableArray();
	}

	private static ImmutableArray<(string Name, string? Description, ImmutableList<(string Type, string Name, string? Description)> Fields)> ParseMessages(Mavlink mavlink)
	{
		return mavlink.Messages.Select(m => (
			m.Name,
			(string?)m.Description,
			m.Field.Select(field =>
			{
				var type = ConvertType(field.Type);
				if (field.Enum is not null)
				{
					type = field.Enum; // We will map enum types later
				}
				return (type, field.Name, (string?)field.Description);
			}).ToImmutableList()
		)).ToImmutableArray();
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
