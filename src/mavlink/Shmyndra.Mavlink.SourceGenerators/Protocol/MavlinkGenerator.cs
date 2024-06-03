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

			context.RegisterSourceOutput(xmlFiles, (sourceProductionContext, files) =>
			{
				ProcessAndReport(sourceProductionContext, files, EnumProcessor.ParseEnums, EnumProcessor.GenerateEnumFile);
				ProcessAndReport(sourceProductionContext, files, MessageProcessor.ParseMessages, MessageProcessor.GenerateMessageFile);
			});
		}

		private static void ProcessAndReport<T>(
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
				// TODO: https://github.com/dotnet/runtime/discussions/102985
				var rootException = ex;
				while (rootException.InnerException is not null)
				{
					rootException = rootException.InnerException;
				}

				if (rootException.Message.Contains("System.ComponentModel.Annotations"))
				{
					// Generation already started
					return;
				}
				else
				{
					context.ReportDiagnostic(
						Diagnostic.Create(
							MavlinkGeneratorDiagnostics.GenericProtocolErrorRule,
							Location.None,
							ex.Message
						)
					);
				}
			}
		}
	}
}
