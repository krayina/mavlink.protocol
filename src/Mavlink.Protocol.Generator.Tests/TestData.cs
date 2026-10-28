using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mavlink.Protocol.Generator.Tests;

/// <summary>
/// Official dialect XMLs and the pymavlink golden metadata, copied next to the test assembly.
/// </summary>
internal static class TestData
{
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

	public static string XmlDirectory => Path.Combine(AppContext.BaseDirectory, "Xml");

	public static TheoryData<string> OfficialXmlNames()
	{
		var data = new TheoryData<string>();
		foreach (var path in Directory.GetFiles(XmlDirectory, "*.xml").Order(StringComparer.Ordinal))
		{
			data.Add(Path.GetFileName(path));
		}

		return data;
	}

	public static TheoryData<string> GoldenXmlNames()
	{
		var data = new TheoryData<string>();
		foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Golden"), "*.json").Order(StringComparer.Ordinal))
		{
			data.Add(Path.GetFileNameWithoutExtension(path) + ".xml");
		}

		return data;
	}

	public static string XmlPath(string fileName) => Path.Combine(XmlDirectory, fileName);

	public static GoldenFile Golden(string xmlFileName)
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Golden", Path.ChangeExtension(xmlFileName, ".json"));
		return JsonSerializer.Deserialize<GoldenFile>(File.ReadAllText(path), JsonOptions)!;
	}
}

internal sealed record GoldenFile(string Source, string Generator, IReadOnlyList<GoldenMessage> Messages);

internal sealed record GoldenMessage(
	uint Id,
	string Name,
	byte CrcExtra,
	int BaseLength,
	int ExtendedLength,
	IReadOnlyList<GoldenField> Fields);

/// <param name="Type">Element type as pymavlink reports it, e.g. <c>uint8_t</c> for <c>uint8_t[4]</c>.</param>
internal sealed record GoldenField(
	string Name,
	string Type,
	int ArrayLength,
	int Offset,
	[property: JsonPropertyName("extension")] bool IsExtension);
