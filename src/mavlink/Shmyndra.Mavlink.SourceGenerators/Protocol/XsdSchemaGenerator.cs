using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml.Schema;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class XsdSchemaGenerator : IIncrementalGenerator
{
	public const string GENERATED_XSD_NAMESPACE = "Mavlink.Shema";
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		new Generator().Generate(context);

	}

	class Generator
	{
		internal void Generate(IncrementalGeneratorInitializationContext context)
		{
			var xsdMavFiles = context.AdditionalTextsProvider
				.Where(x => x.Path.EndsWith(".xsd"))
				.Select((text, _) => GenerateTypesFromXsd(text));

			context.RegisterSourceOutput(xsdMavFiles, (context, source) =>
			{
				context.AddSource("XsdMavlinkSchema.g.cs", SourceText.From(source, Encoding.UTF8));
			});
		}

		private string GenerateTypesFromXsd(AdditionalText xsdAdditionalText)
		{
			XmlSchemaSet schemas = new XmlSchemaSet();
			schemas.Add("", xsdAdditionalText.Path);

			var outputWriter = new InMemoryOutputWriter();

			var generator = new XmlSchemaClassGenerator.Generator
			{
				NamespaceProvider = new NamespaceProvider
				{
					GenerateNamespace = key =>
					{
						return GENERATED_XSD_NAMESPACE;
					}
				},
				OutputWriter = outputWriter,
			};

			var set = new XmlSchemaSet();
			set.Add("", xsdAdditionalText.Path);
			generator.Generate(set);

			var source = outputWriter.GetContents().First();
			var syntaxTree = CSharpSyntaxTree.ParseText(source);
			var normalizedCode = syntaxTree.GetRoot().NormalizeWhitespace().ToFullString();
			return normalizedCode;
		}
	}
}

public class InMemoryOutputWriter : OutputWriter
{
	private readonly Dictionary<string, StringBuilder> _files = new Dictionary<string, StringBuilder>();
	private StringBuilder? _current;

	public override void Write(CodeNamespace cn)
	{
		var cu = new CodeCompileUnit();
		cu.Namespaces.Add(cn);

		using var writer = new StringWriter();
		Write(writer, cu);

		var fileName = $"{cn.Name}.cs";
		if (!_files.TryGetValue(fileName, out _current))
		{
			_current = new StringBuilder();
			_files[fileName] = _current;
		}
		_current.Append(writer.ToString());
	}

	public List<string> GetContents()
	{
		var contents = new List<string>();
		foreach (var file in _files)
		{
			contents.Add(file.Value.ToString());
		}
		return contents;
	}
}
