using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Parsing;

/// <summary>
/// Reads one dialect XML into a <see cref="ParsedFile"/>. Malformed elements are reported
/// as MAVGEN001 and skipped; reading goes on, so one run shows every problem in the file.
/// </summary>
internal static class MavlinkXmlReader
{
	private static readonly XmlReaderSettings Settings = new()
	{
		DtdProcessing = DtdProcessing.Prohibit,
		XmlResolver = null,
	};

	public static ParsedFile Read(string path, SourceText text, CancellationToken cancellationToken)
	{
		var content = text.ToString();
		var context = new ReadContext(path, text);

		XDocument document;
		try
		{
			using var reader = XmlReader.Create(new StringReader(content), Settings);
			document = XDocument.Load(reader, LoadOptions.SetLineInfo);
		}
		catch (XmlException exception)
		{
			context.Report(exception.LineNumber, exception.LinePosition, 1, exception.Message);
			return context.ToFile(XmlContentHash.Compute(content), null, null);
		}

		var root = document.Root!;
		if (root.Name.LocalName != "mavlink")
		{
			context.Report(root, $"The root element must be <mavlink>, not <{root.Name.LocalName}>.");
			return context.ToFile(XmlContentHash.Compute(content), null, null);
		}

		foreach (var element in root.Elements("include"))
		{
			var include = element.Value.Trim();
			if (include.Length == 0)
			{
				context.Report(element, "<include> must name a file.");
				continue;
			}

			context.Includes.Add(new ParsedInclude(include, context.Location(element)));
		}

		var version = ReadByteElement(context, root.Element("version"));
		var dialect = ReadByteElement(context, root.Element("dialect"));

		foreach (var element in root.Elements("enums").Elements("enum"))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (ReadEnum(context, element) is { } parsed)
			{
				context.Enums.Add(parsed);
			}
		}

		foreach (var element in root.Elements("messages").Elements("message"))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (ReadMessage(context, element) is { } parsed)
			{
				context.Messages.Add(parsed);
			}
		}

		return context.ToFile(XmlContentHash.Compute(content), version, dialect);
	}

	private static ParsedEnum? ReadEnum(ReadContext context, XElement element)
	{
		if (!context.TryGetRequired(element, "name", out var name))
		{
			return null;
		}

		var entries = new List<ParsedEntry>();
		foreach (var child in element.Elements("entry"))
		{
			if (ReadEntry(context, child) is { } entry)
			{
				entries.Add(entry);
			}
		}

		return new ParsedEnum(
			name,
			context.GetBoolean(element, "bitmask"),
			element.Element("description")?.Value,
			ReadStatus(element),
			entries.ToEquatableArray(),
			context.Location(element));
	}

	private static ParsedEntry? ReadEntry(ReadContext context, XElement element)
	{
		if (!context.TryGetRequired(element, "name", out var name))
		{
			return null;
		}

		ulong? value = null;
		if (element.Attribute("value") is { } valueAttribute)
		{
			if (!TryParseEntryValue(valueAttribute.Value, out var parsed))
			{
				context.Report(valueAttribute, $"Entry '{name}' has value '{valueAttribute.Value}'; expected a non-negative decimal or 0x-prefixed hexadecimal number.");
				return null;
			}

			value = parsed;
		}

		var flags = ParsedEntryFlags.None;
		flags |= context.GetBoolean(element, "hasLocation") ? ParsedEntryFlags.HasLocation : 0;
		flags |= context.GetBoolean(element, "isDestination") ? ParsedEntryFlags.IsDestination : 0;
		flags |= context.GetBoolean(element, "hasAltitudeOnly") ? ParsedEntryFlags.HasAltitudeOnly : 0;
		flags |= context.GetBoolean(element, "missionOnly") ? ParsedEntryFlags.MissionOnly : 0;
		flags |= context.GetBoolean(element, "mission") ? ParsedEntryFlags.Mission : 0;
		flags |= context.GetBoolean(element, "fence") ? ParsedEntryFlags.Fence : 0;
		flags |= context.GetBoolean(element, "rally") ? ParsedEntryFlags.Rally : 0;
		flags |= context.GetBoolean(element, "command") ? ParsedEntryFlags.Command : 0;

		var parameters = new List<ParsedParam>();
		foreach (var child in element.Elements("param"))
		{
			if (ReadParam(context, child) is { } parameter)
			{
				parameters.Add(parameter);
			}
		}

		return new ParsedEntry(
			name,
			value,
			element.Element("description")?.Value,
			ReadStatus(element),
			flags,
			parameters.ToEquatableArray(),
			context.Location(element));
	}

	private static ParsedParam? ReadParam(ReadContext context, XElement element)
	{
		if (!context.TryGetRequired(element, "index", out var indexText))
		{
			return null;
		}

		if (!byte.TryParse(indexText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
		{
			context.Report(element.Attribute("index")!, $"Param index '{indexText}' must be a number from 0 to 255.");
			return null;
		}

		return new ParsedParam(
			index,
			element.Value,
			Optional(element, "label"),
			Optional(element, "units"),
			Optional(element, "multiplier"),
			context.GetBoolean(element, "instance"),
			Optional(element, "enum"),
			Optional(element, "decimalPlaces"),
			Optional(element, "increment"),
			Optional(element, "minValue"),
			Optional(element, "maxValue"),
			context.GetBoolean(element, "reserved"),
			Optional(element, "default"),
			context.Location(element));
	}

	private static ParsedMessage? ReadMessage(ReadContext context, XElement element)
	{
		var hasName = context.TryGetRequired(element, "name", out var name);
		var hasId = context.TryGetRequired(element, "id", out var idText);
		if (!hasName || !hasId)
		{
			return null;
		}

		if (!uint.TryParse(idText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
		{
			context.Report(element.Attribute("id")!, $"Message '{name}' has id '{idText}'; expected a non-negative decimal number.");
			return null;
		}

		// A message with a broken field cannot be serialized correctly, so it is dropped as a whole.
		var fields = new List<ParsedField>();
		var valid = true;
		var isExtension = false;
		foreach (var child in element.Elements())
		{
			switch (child.Name.LocalName)
			{
				case "extensions":
					isExtension = true;
					break;
				case "field":
					if (ReadField(context, child, isExtension) is { } field)
					{
						fields.Add(field);
					}
					else
					{
						valid = false;
					}

					break;
			}
		}

		if (!valid)
		{
			return null;
		}

		return new ParsedMessage(
			id,
			name,
			element.Element("description")?.Value,
			ReadStatus(element),
			fields.ToEquatableArray(),
			context.Location(element));
	}

	private static ParsedField? ReadField(ReadContext context, XElement element, bool isExtension)
	{
		var hasType = context.TryGetRequired(element, "type", out var type);
		var hasName = context.TryGetRequired(element, "name", out var name);
		if (!hasType || !hasName)
		{
			return null;
		}

		return new ParsedField(
			type,
			name,
			isExtension,
			element.Value,
			Optional(element, "enum"),
			Optional(element, "display"),
			Optional(element, "units"),
			Optional(element, "multiplier"),
			Optional(element, "print_format"),
			Optional(element, "invalid"),
			Optional(element, "default"),
			context.GetBoolean(element, "instance"),
			Optional(element, "increment"),
			Optional(element, "minValue"),
			Optional(element, "maxValue"),
			context.Location(element));
	}

	private static ParsedStatus? ReadStatus(XElement element)
	{
		foreach (var child in element.Elements())
		{
			var kind = child.Name.LocalName switch
			{
				"deprecated" => ParsedStatusKind.Deprecated,
				"superseded" => ParsedStatusKind.Superseded,
				"wip" => ParsedStatusKind.Wip,
				_ => (ParsedStatusKind?)null,
			};

			if (kind is { } found)
			{
				var description = child.Value;
				return new ParsedStatus(
					found,
					Optional(child, "since"),
					Optional(child, "replaced_by"),
					Optional(child, "remove_on_date"),
					description.Length == 0 ? null : description);
			}
		}

		return null;
	}

	private static byte? ReadByteElement(ReadContext context, XElement? element)
	{
		if (element is null)
		{
			return null;
		}

		if (!byte.TryParse(element.Value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
		{
			context.Report(element, $"<{element.Name.LocalName}> must be a number from 0 to 255, not '{element.Value}'.");
			return null;
		}

		return value;
	}

	internal static bool TryParseEntryValue(string text, out ulong value)
	{
		text = text.Trim();
		if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
		{
			return ulong.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
		}

		return ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
	}

	private static string? Optional(XElement element, string name) => element.Attribute(name)?.Value;

	private sealed class ReadContext
	{
		private readonly string _path;
		private readonly SourceText _text;
		private readonly List<DiagnosticInfo> _diagnostics = new();

		public ReadContext(string path, SourceText text)
		{
			_path = path;
			_text = text;
		}

		public List<ParsedInclude> Includes { get; } = new();

		public List<ParsedEnum> Enums { get; } = new();

		public List<ParsedMessage> Messages { get; } = new();

		public ParsedFile ToFile(string hash, byte? version, byte? dialect)
			=> new(
				_path,
				hash,
				Includes.ToEquatableArray(),
				version,
				dialect,
				Enums.ToEquatableArray(),
				Messages.ToEquatableArray(),
				_diagnostics.ToEquatableArray());

		// Points at the element or attribute name, which is where IXmlLineInfo positions are.
		public LocationInfo Location(XElement element) => Location(element, element.Name.LocalName.Length);

		public LocationInfo Location(XAttribute attribute) => Location(attribute, attribute.Name.LocalName.Length);

		public bool TryGetRequired(XElement element, string attribute, out string value)
		{
			if (element.Attribute(attribute) is { } found)
			{
				value = found.Value;
				return true;
			}

			Report(element, $"<{element.Name.LocalName}> is missing the required attribute '{attribute}'.");
			value = string.Empty;
			return false;
		}

		public bool GetBoolean(XElement element, string name)
		{
			if (element.Attribute(name) is not { } attribute)
			{
				return false;
			}

			switch (attribute.Value.Trim())
			{
				case "true":
				case "1":
					return true;
				case "false":
				case "0":
					return false;
				default:
					Report(attribute, $"Attribute '{name}' must be 'true' or 'false', not '{attribute.Value}'.");
					return false;
			}
		}

		public void Report(XElement element, string message) => Add(Location(element), message);

		public void Report(XAttribute attribute, string message) => Add(Location(attribute), message);

		public void Report(int line, int column, int length, string message)
			=> Add(LocationInfo.FromLineColumn(_path, _text, line, column, length), message);

		private LocationInfo Location(IXmlLineInfo node, int length)
			=> LocationInfo.FromLineColumn(_path, _text, node.LineNumber, node.LinePosition, length);

		private void Add(LocationInfo location, string message)
			=> _diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.InvalidXml, location, message));
	}
}
