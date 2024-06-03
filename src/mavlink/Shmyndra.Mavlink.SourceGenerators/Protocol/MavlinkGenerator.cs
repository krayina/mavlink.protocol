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
				ProcessFiles(sourceProductionContext, files, EnumProcessor.ParseEnums, EnumProcessor.GenerateEnumFile);
				ProcessFiles(sourceProductionContext, files, MessageProcessor.ParseMessages, MessageProcessor.GenerateMessageFile);
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
				HandleException(context, ex);
			}
		}

		private static void HandleException(SourceProductionContext context, Exception ex)
		{
			var rootException = GetRootException(ex);

			if (rootException.Message.Contains("System.ComponentModel.Annotations"))
			{
				// Generation already started
				// TODO: https://github.com/dotnet/runtime/discussions/102985
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create(
					MavlinkGeneratorDiagnostics.GenericProtocolErrorRule,
					Location.None,
					ex.Message
				)
			);
		}

		private static Exception GetRootException(Exception ex)
		{
			var rootException = ex;
			while (rootException.InnerException is not null)
			{
				rootException = rootException.InnerException;
			}
			return rootException;
		}
	}
}
