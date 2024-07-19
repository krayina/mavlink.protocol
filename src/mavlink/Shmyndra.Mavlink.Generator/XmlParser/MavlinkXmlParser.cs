#if false
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkParser
{
	MavlinkData Parse(string content);
}

public class MavlinkXmlParser : IMavlinkParser
{
	private static readonly Dictionary<string, (string TypeName, int Size)> _typeMap = new()
	{
		{ "char", ("char", 1) },
		{ "uint8_t", ("byte", 1) },
		{ "int8_t", ("sbyte", 1) },
		{ "uint16_t", ("ushort", 2) },
		{ "int16_t", ("short", 2) },
		{ "uint32_t", ("uint", 4) },
		{ "int32_t", ("int", 4) },
		{ "uint64_t", ("ulong", 8) },
		{ "int64_t", ("long", 8) },
		{ "float", ("float", 4) },
		{ "double", ("double", 8) },
		{ "uint8_t_mavlink_version", ("byte", 1) }
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

	private ImmutableArray<(string Name, string? Description, bool Bitmask, ImmutableList<(string Name, string Value, string? Description)> Entries)> ParseEnums(Mavlink mavlink)
	{
		return mavlink.Enums.Select(e => (
			e.Name,
			e.Description,
			e.Bitmask,
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
					var (typeName, size) = _typeMap[field.Type];
					type = new FieldEnumType(field.Enum, size);
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
				return new FieldArrayType($"System.Collections.Immutable.ImmutableArray<{mappedBaseType.TypeName}>", arraySize);
			}
		}
		else if (_typeMap.TryGetValue(xmlType, out var type))
		{
			return new FieldType(type.TypeName);
		}

		return new FieldType("object");
	}
}
#endif
