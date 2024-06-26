using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class MavlinkGenerator : IIncrementalGenerator
{
	private readonly IMavlinkEnumTypesGenerator _enumGenerator;
	private readonly IMavlinkMessageTypesGenerator _messageGenerator;
	private readonly IMavlinkSpecificationTypeGenerator _specificationGenerator;

	public MavlinkGenerator()
		: this(new MavlinkEnumTypesGenerator(), new MavlinkMessageTypesGenerator(), new MavlinkSpecificationTypeGenerator())
	{
	}

	public MavlinkGenerator(
		IMavlinkEnumTypesGenerator enumGenerator,
		IMavlinkMessageTypesGenerator messageGenerator,
		IMavlinkSpecificationTypeGenerator specificationGenerator)
	{
		_enumGenerator = enumGenerator;
		_messageGenerator = messageGenerator;
		_specificationGenerator = specificationGenerator;
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Launch();
		}

		var contentGenerator = new MavlinkTypesGenerator(_enumGenerator, _messageGenerator, _specificationGenerator);
		Generate(context, contentGenerator);
	}

	private void Generate(IncrementalGeneratorInitializationContext context, MavlinkTypesGenerator contentGenerator)
	{
		var additionalTexts = context.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));
		var xmlFiles = additionalTexts
			.Select((file, _) => new
			{
				file.Path,
				Content = file.GetText()!.ToString()
			}).Collect();

		context.RegisterSourceOutput(xmlFiles, (sourceProductionContext, files) =>
		{
			try
			{
				var fileContents = files.ToDictionary(f => f.Path, f => f.Content);
				var orderedFiles = MavlinkXmlIncludeOrderer.GetOrderedFiles(fileContents);

				foreach (var xmlFile in orderedFiles)
				{
					var content = fileContents[xmlFile];
					var mavlinkData = MavlinkXmlParser.Parse(content);
					var namespaceName = $"MavlinkTypes.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(xmlFile))}";

					var members = contentGenerator.GenerateNamespaceMembers(mavlinkData, namespaceName);
					AddSource(sourceProductionContext, namespaceName, members);
				}
			}
			catch (Exception ex)
			{
				ExceptionHandler.HandleException(sourceProductionContext, ex);
			}
		});
	}

	private void AddSource(SourceProductionContext context, string namespaceName, List<MemberDeclarationSyntax> members)
	{
		var compilationUnit = SyntaxFactory.CompilationUnit()
			.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
				.AddMembers(members.ToArray()));

		context.AddSource($"{namespaceName}.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
	}
}
