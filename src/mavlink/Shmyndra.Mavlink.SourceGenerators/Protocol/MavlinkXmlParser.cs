using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public record FieldType(string TypeName);
public record FieldArrayType(string TypeName, int Length) : FieldType(TypeName);

public record MavlinkData(
	ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> Enums,
	ImmutableArray<(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> Messages,
	byte? Version,
	byte? Dialect);

public static class MavlinkXmlParser
{
	private static readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>
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
			mavlink.VersionSpecified ? mavlink.Version : null,
			mavlink.DialectSpecified ? mavlink.Dialect : null);
	}

	private static ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> ParseEnums(Mavlink mavlink)
	{
		return mavlink.Enums.Select(e => (
			e.Name,
			e.Description,
			e.Entry.Select(entry => (entry.Name, entry.Value, (string?)entry.Description)).ToImmutableList()
		)).ToImmutableArray();
	}

	private static ImmutableArray<(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> ParseMessages(Mavlink mavlink)
	{
		return mavlink.Messages.Select(m => (
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

	private static FieldType ConvertToFieldType(string xmlType)
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
