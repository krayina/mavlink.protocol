using System.Collections.Immutable;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class MavlinkGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		new Generator().Generate(context);
	}

	class Generator
	{
		internal void Generate(IncrementalGeneratorInitializationContext context)
		{
			var additionalTexts = context.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));
			var xmlFiles = additionalTexts.Select((file, _) => file.GetText()!.ToString()).Collect();

			var enums = xmlFiles.SelectMany((files, _) => ParseEnums(files).ToImmutableArray());
			var messages = xmlFiles.SelectMany((files, _) => ParseMessages(files).ToImmutableArray());

			context.RegisterSourceOutput(enums.Collect(), GenerateEnumFile);
			context.RegisterSourceOutput(messages.Collect(), GenerateMessageFile);
		}

		private static IEnumerable<(string Name, string Description, List<(string Name, string Value, string Description)> Entries)> ParseEnums(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var e in mavlink.Enums)
				{
					yield return (ToCamelCase(e.Name), e.Description ?? "No description available", e.Entry.Select(entry => (ToCamelCase(entry.Name), entry.Value, entry.Description ?? "No description available")).ToList());
				}
			}
		}

		private static IEnumerable<(string Name, string Description, List<(string Type, string Name, string Description)> Fields)> ParseMessages(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var m in mavlink.Messages)
				{
					yield return (ToCamelCase(m.Name), m.Description ?? "No description available", m.Field.Select(field => (ConvertType(field.Type), ToCamelCase(field.Name), field.Description ?? "No description available")).ToList());
				}
			}
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

		private static void GenerateEnumFile(SourceProductionContext context, ImmutableArray<(string Name, string Description, List<(string Name, string Value, string Description)> Entries)> enums)
		{
			if (enums.IsDefaultOrEmpty)
				return;

			var enumSource = new StringBuilder(@"
namespace GeneratedMavlink
{
");
			foreach (var @enum in enums)
			{
				enumSource.AppendLine($@"
    /// <summary>
    /// {@enum.Description}
    /// </summary>
    public enum {@enum.Name}
    {{
        {string.Join(",\n        ", @enum.Entries.Select(entry => $@"
        /// <summary>
        /// {entry.Description}
        /// </summary>
        {entry.Name} = {entry.Value}"))}
    }}
");
			}

			enumSource.AppendLine("}");

			context.AddSource("MavlinkEnums.g.cs", SourceText.From(enumSource.ToString(), Encoding.UTF8));
		}

		private static void GenerateMessageFile(SourceProductionContext context, ImmutableArray<(string Name, string Description, List<(string Type, string Name, string Description)> Fields)> messages)
		{
			if (messages.IsDefaultOrEmpty)
				return;

			var messageSource = new StringBuilder(@"
namespace GeneratedMavlink
{
");
			foreach (var message in messages)
			{
				messageSource.AppendLine($@"
    /// <summary>
    /// {message.Description}
    /// </summary>
    public record struct {message.Name}
    {{
        {string.Join("\n        ", message.Fields.Select(field => $@"
        /// <summary>
        /// {field.Description}
        /// </summary>
        public {field.Type} {field.Name} {{ get; init; }}"))}
    }}
");
			}

			messageSource.AppendLine("}");

			context.AddSource("MavlinkMessages.g.cs", SourceText.From(messageSource.ToString(), Encoding.UTF8));
		}

		private static string ToCamelCase(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < words.Length; i++)
			{
				words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
			}

			return string.Join("", words);
		}
	}
}
