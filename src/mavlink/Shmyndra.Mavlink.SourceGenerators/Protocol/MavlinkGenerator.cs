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
				var fileContents = files.ToDictionary(f => f.Path, f => f.Content);
				var orderedFiles = MavlinkXmlIncludeOrderer.GetOrderedFiles(fileContents);

				var xmlContent = orderedFiles.Select(path => fileContents[path]).ToImmutableArray();

				ProcessFiles(sourceProductionContext, xmlContent, EnumProcessor.ParseEnums, EnumProcessor.GenerateEnumFile);
				ProcessFiles(sourceProductionContext, xmlContent, MessageProcessor.ParseMessages, MessageProcessor.GenerateMessageFile);
			});
		}

		private static void ProcessFiles<T>(
			SourceProductionContext context,
			ImmutableArray<string> files,
			Func<IEnumerable<string>, IEnumerable<T>> parseFunc,
			Action<SourceProductionContext, ImmutableArray<T>> generateFunc)
		{
			try
			{
				var items = parseFunc(files).ToImmutableArray();
				generateFunc(context, items);
			}
			catch (Exception ex)
			{
				ExceptionHandler.HandleException(context, ex);
			}
		}
	}
}
