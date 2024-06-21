using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class MavlinkGenerator : IIncrementalGenerator
{
	private readonly IMavlinkEnumTypesGenerator _enumGenerator;
	private readonly IMavlinkMessageTypesGenerator _messageGenerator;

	public MavlinkGenerator()
		: this(new MavlinkEnumTypesGenerator(), new MavlinkMessageTypesGenerator())
	{
	}

	public MavlinkGenerator(IMavlinkEnumTypesGenerator enumGenerator, IMavlinkMessageTypesGenerator messageGenerator)
	{
		_enumGenerator = enumGenerator;
		_messageGenerator = messageGenerator;
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			System.Diagnostics.Debugger.Launch();
		}
		new Generator(_enumGenerator, _messageGenerator).Generate(context);
	}

	class Generator
	{
		private readonly IMavlinkEnumTypesGenerator _enumGenerator;
		private readonly IMavlinkMessageTypesGenerator _messageGenerator;

		public Generator(IMavlinkEnumTypesGenerator enumGenerator, IMavlinkMessageTypesGenerator messageGenerator)
		{
			_enumGenerator = enumGenerator;
			_messageGenerator = messageGenerator;
		}

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

					var allGeneratedEnumTypes = new Dictionary<string, (string Namespace, string TypeName)>();
					var allGeneratedMessageTypes = new Dictionary<string, (string Namespace, string TypeName)>();

					foreach (var xmlFile in orderedFiles)
					{
						var content = fileContents[xmlFile];
						var namespaceName = $"MavlinkTypes.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(xmlFile))}";

						var enums = MavlinkXmlEnumParser.ParseEnums(new[] { content }).ToImmutableArray();
						var enumDeclarations = _enumGenerator.GenerateEnums(enums, namespaceName, out var generatedMavlinkEnumTypes);

						foreach (var kvp in generatedMavlinkEnumTypes)
						{
							allGeneratedEnumTypes[kvp.Key] = kvp.Value;
						}

						var messages = MavlinkXmlMessageParser.ParseMessages(new[] { content }, allGeneratedEnumTypes.ToImmutableDictionary()).ToImmutableArray();
						var messageDeclarations = _messageGenerator.GenerateMessages(messages, namespaceName, allGeneratedEnumTypes.ToImmutableDictionary(), out var generatedMavlinkMessageTypes);

						foreach (var kvp in generatedMavlinkMessageTypes)
						{
							allGeneratedMessageTypes[kvp.Key] = kvp.Value;
						}

						var compilationUnit = SyntaxFactory.CompilationUnit()
							.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
								.AddMembers(enumDeclarations.ToArray())
								.AddMembers(messageDeclarations.ToArray()));

						// Діагностичне повідомлення
						System.Diagnostics.Debug.WriteLine($"Generated code for {namespaceName}");

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
