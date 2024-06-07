using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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

					var enums = EnumProcessor.ParseEnums(xmlContent).ToImmutableArray();
					var generatedMavlinkEnumTypes = EnumProcessor.GenerateEnumFile(sourceProductionContext, enums);

					var messages = MessageProcessor.ParseMessages(xmlContent, generatedMavlinkEnumTypes).ToImmutableArray();
					var generatedMavlinkMessageTypes = MessageProcessor.GenerateMessageFile(sourceProductionContext, messages);
				}
				catch (Exception ex)
				{
					ExceptionHandler.HandleException(sourceProductionContext, ex);
				}
			});
		}
	}
}
