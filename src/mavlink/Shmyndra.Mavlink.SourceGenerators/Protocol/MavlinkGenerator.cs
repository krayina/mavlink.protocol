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
			var xmlFiles = additionalTexts.Select((file, _) => file.GetText()!.ToString()).Collect();

			var enums = xmlFiles.SelectMany((files, _) => EnumProcessor.ParseEnums(files).ToImmutableArray());
			var messages = xmlFiles.SelectMany((files, _) => MessageProcessor.ParseMessages(files).ToImmutableArray());

			context.RegisterSourceOutput(enums.Collect(), EnumProcessor.GenerateEnumFile);
			context.RegisterSourceOutput(messages.Collect(), MessageProcessor.GenerateMessageFile);
		}
	}
}
