using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkXmlParser
{
	MavlinkData Parse(string xmlContent);
}

public class MavlinkXmlParser : IMavlinkXmlParser
{
	private static readonly Dictionary<string, string> _typeMap = new()
	{
		{ "char", "char" },
		{ "uint8_t", "byte" },
		{ "int8_t", "sbyte" },
		{ "uint16_t", "ushort" },
		{ "int16_t", "short" },
		{ "uint32_t", "uint" },
		{ "int32_t", "int" },
		{ "uint64_t", "ulong" },
		{ "int64_t", "long" },
		{ "float", "float" },
		{ "double", "double" },
		{ "uint8_t_mavlink_version", "byte" }
	};

	public MavlinkData Parse(string xmlContent)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		using var reader = new StringReader(xmlContent);
		var mavlink = (Mavlink)serializer.Deserialize(reader);

		var enums = ParseEnums(mavlink);
		var messages = ParseMessages(mavlink);
		var includes = ParseIncludes(mavlink);

		return new MavlinkData(
			enums,
			messages,
			includes,
			mavlink.VersionSpecified ? mavlink.Version : null,
			mavlink.DialectSpecified ? mavlink.Dialect : null);
	}

	private ImmutableArray<string> ParseIncludes(Mavlink mavlink)
	{
		return mavlink.Include.Select(i => i.Trim()).ToImmutableArray();
	}

	private ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> ParseEnums(Mavlink mavlink)
	{
		return mavlink.Enums.Select(e => (
			e.Name,
			e.Description,
			e.Entry.Select(entry => (entry.Name, entry.Value, (string?)entry.Description)).ToImmutableList()
		)).ToImmutableArray();
	}

	private ImmutableArray<(uint Id, string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> ParseMessages(Mavlink mavlink)
	{
		return mavlink.Messages.Select(m => (
			m.Id,
			m.Name,
			(string?)m.Description,
			m.Field.Select(field =>
			{
				var type = ConvertToFieldType(field.Type);

				if (field.Enum is not null)
				{
					type = new FieldType(field.Enum);
				}
				return (type, field.Name, (string?)field.Description);
			}).ToImmutableList()
		)).ToImmutableArray();
	}

	private FieldType ConvertToFieldType(string xmlType)
	{
		if (xmlType.EndsWith("]"))
		{
			var baseType = xmlType.Substring(0, xmlType.IndexOf('['));
			var arraySize = int.Parse(xmlType.Substring(xmlType.IndexOf('[')).Trim('[', ']'));

			if (_typeMap.TryGetValue(baseType, out var mappedBaseType))
			{
				return new FieldArrayType($"System.Collections.Immutable.ImmutableArray<{mappedBaseType}>", arraySize);
			}
		}
		else if (_typeMap.TryGetValue(xmlType, out var type))
		{
			return new FieldType(type);
		}

		return new FieldType("object");
	}
}
