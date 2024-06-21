using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class MavlinkGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Launch();
		}
		new Generator().Generate(context);
	}

	class Generator
	{
		internal void Generate(IncrementalGeneratorInitializationContext context)
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
					var xmlContent = orderedFiles.Select(path => fileContents[path]).ToImmutableArray();

					foreach (var xmlFile in orderedFiles)
					{
						var content = fileContents[xmlFile];
						var namespaceName = $"MavlinkTypes.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(xmlFile))}";

						var enums = EnumProcessor.ParseEnums(new[] { content }).ToImmutableArray();
						var generatedMavlinkEnumTypes = enums.ToImmutableDictionary(e => e.Name, e => (namespaceName, Utilities.ToCamelCase(e.Name)));

						var messages = MessageProcessor.ParseMessages(new[] { content }, generatedMavlinkEnumTypes).ToImmutableArray();
						var generatedMavlinkMessageTypes = messages.ToImmutableDictionary(m => m.Name, m => (namespaceName, Utilities.ToCamelCase(m.Name)));

						var allGeneratedTypes = generatedMavlinkEnumTypes.AddRange(generatedMavlinkMessageTypes);

						var compilationUnit = SyntaxFactory.CompilationUnit()
							.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
								.AddMembers(enums.Select(enumData => EnumProcessor.CreateEnum(enumData)).ToArray())
								.AddMembers(messages.Select(messageData => MessageProcessor.CreateRecordStruct(messageData, namespaceName, allGeneratedTypes)).ToArray()));

						sourceProductionContext.AddSource($"{namespaceName}.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
					}
				}
				catch (Exception ex)
				{
					ExceptionHandler.HandleException(sourceProductionContext, ex);
				}
			});
		}
	}
}
