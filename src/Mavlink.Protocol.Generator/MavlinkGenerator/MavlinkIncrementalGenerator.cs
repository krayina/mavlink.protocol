using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator;

[Generator]
public class MavlinkIncrementalGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			//Uncomment this line to enter Debug mode for the generator and perform a Rebuild
			//System.Diagnostics.Debugger.Launch();
		}

		try
		{
			new Generator().Generate(context);
		}
		catch (Exception)
		{
			//context.ReportDiagnostic(
			//	Diagnostic.Create(
			//		MavlinkGeneratorDiagnostics.GenericProtocolErrorRule,
			//		Location.None,
			//		ex.Message
			//	)
			//);
		}
	}

	class Generator
	{
		private static readonly IMavlinkTreeBuilder _treeBuilder = new MavlinkTreeBuilder(new MavlinkXmlParser());

		internal void Generate(IncrementalGeneratorInitializationContext context)
		{
			var xmlFiles = context.AdditionalTextsProvider
				.Where(file => file.Path.EndsWith(".xml")
					&& !Path.GetFileName(file.Path).StartsWith("_"))
				.Select((file, _) => (file.Path, Content: file.GetText()!.ToString()))
				.Collect();

			var compilationAndFiles = context.CompilationProvider.Combine(xmlFiles);
			context.RegisterSourceOutput(compilationAndFiles, GenerateSourceFiles);
		}

		private void GenerateSourceFiles(SourceProductionContext spc, (Compilation Compilation, ImmutableArray<(string Path, string Content)> Files) input)
		{
			var (compilation, files) = input;

			var enumGenerator = new MavlinkEnumGenerator();
			var messageGenerator = GetMavlinkMessageGeneratorInstanceWithNetStandardCondition(compilation);

			var enumTreeGenerator = new MavlinkEnumTreeGenerator(enumGenerator);

			var generator = new MavlinkGenerator(
				_treeBuilder,
				enumGenerator,
				enumTreeGenerator,
				messageGenerator,
				new MavlinkSpecificationGenerator());

			var filesDictionary = files.ToImmutableDictionary(item => item.Path, item => item.Content);
			var generatedFiles = generator.GenerateMavlink(filesDictionary);

			foreach (var generatedFile in generatedFiles.Values)
			{
				AddSource(spc, generatedFile.Namespace, generatedFile.Syntax);
			}

			var messagesStorage = (IGeneratedStorage<GeneratedMavlinkMessage>)messageGenerator;
			var enumsStorage = (IGeneratedStorage<GeneratedMavlinkEnum>)enumGenerator;

			var generatedMessages = messagesStorage.GetGeneratedTypes();
			var generatedEnums = enumsStorage.GetGeneratedTypes();

			var generatedMessagesSourceCode = MavlinkMessagesGenerator.GenerateMessageExtensions(generatedMessages);

			var enumBitmaskNamespaceBuilder = new CompilationUnitBuilder(MavlinkGeneratorConstants.TypesNamespace);
			var mavlinkGenericEnumBitmaskGenerator = new MavlinkGenericEnumBitmaskGenerator();
			var mavlinkSpecificEnumBitmaskGenerator = new MavlinkSpecificEnumBitmaskGenerator();
			foreach (var generatedEnum in generatedEnums)
			{
				if (generatedEnum.Original.Bitmask == true)
				{
					var generatedGenericEnumBitmask = mavlinkGenericEnumBitmaskGenerator.Generate(generatedEnum);
					enumBitmaskNamespaceBuilder.AddMember(generatedGenericEnumBitmask);
				}
			}

			(GeneratedMavlinkEnum GeneratedEnum, string UnderlyingType)[] underlyingEnumDependencies = generatedMessages
				.SelectMany(x => x.GeneratedFields)
				.Where(x => x.Original.Display == MavlinkMessageFieldDisplay.Bitmask &&
							(x.GeneratedType is GeneratedMavlinkMessageFieldEnumType ||
							 x.GeneratedType is GeneratedMavlinkMessageFieldArrayType { ElementType: GeneratedMavlinkMessageFieldEnumType }))
				.Select(x => x.GeneratedType.GetElementTypeOrSelf() as GeneratedMavlinkMessageFieldEnumType)
				.Where(x => x != null)
				.Select(x => (x!.GeneratedEnum, x.ConvertedType))
				.Distinct()
				.ToArray();

			foreach (var item in underlyingEnumDependencies)
			{
				var generatedSpecificEnumBitmaskType = mavlinkSpecificEnumBitmaskGenerator.Generate(item.GeneratedEnum, item.UnderlyingType);
				enumBitmaskNamespaceBuilder.AddMember(generatedSpecificEnumBitmaskType);
			}

			AddSource(spc, MavlinkGeneratorConstants.TypesNamespace, generatedMessagesSourceCode);
			AddSource(spc, "EnumBitmaskTypes", enumBitmaskNamespaceBuilder.Build());
		}

		private MavlinkMessageGenerator GetMavlinkMessageGeneratorInstanceWithNetStandardCondition(Compilation compilation)
		{
			bool supportsSpan = IsSpanSerializationAvailable(compilation);
			bool useObjectiveBitmask = IsObjectiveBitmaskEnabled(compilation);

			var ruleDefinitionProvider = new MavlinkMessageFieldValidationRuleDefinitionProvider();
			var placementProvider = new InvalidatabilityPlacementProvider();
			var invalidValueBuilder = new InvalidValueExpressionBuilder();
			var validationCompiler = new MavlinkMessageFieldValidationExpressionCompiler(invalidValueBuilder);

			IMavlinkMessageFieldTypeNameResolutionStrategy bitmaskTypeNameStrategy = useObjectiveBitmask
				? new MavlinkObjectiveBitmaskFieldTypeNameResolutionStrategy()
				: new MavlinkBitmaskFieldTypeNameResolutionStrategy();

			var typeNameResolver = new MavlinkFieldTypeNameResolverFacade(
				bitmaskTypeNameStrategy,
				new MavlinkNonBitmaskFieldTypeNameResolutionStrategy());

			MavlinkMessageDeserializationMethodGenerator deserializationGenerator;
			MavlinkMessageSerializationMethodGenerator serializationGenerator;

			if (supportsSpan)
			{
				deserializationGenerator = new MavlinkMessageSpanDeserializationMethodGenerator(
					validationCompiler,
					useObjectiveBitmask);

				serializationGenerator = new MavlinkMessageSpanSerializationMethodGenerator(
					invalidValueBuilder,
					useObjectiveBitmask);
			}
			else
			{
				deserializationGenerator = new MavlinkMessageBufferDeserializationMethodGenerator(
					validationCompiler,
					useObjectiveBitmask);

				serializationGenerator = new MavlinkMessageBufferSerializationMethodGenerator(
					invalidValueBuilder,
					useObjectiveBitmask);
			}

			return new MavlinkMessageGenerator(
				new MavlinkMessageFieldInitPropertyGenerator(typeNameResolver, ruleDefinitionProvider, placementProvider),
				deserializationGenerator,
				serializationGenerator
			);
		}

		private static bool IsObjectiveBitmaskEnabled(Compilation compilation)
		{
			return compilation.SyntaxTrees.FirstOrDefault()?.Options is CSharpParseOptions options &&
				   options.PreprocessorSymbolNames.Contains("USE_OBJECTIVE_BITMASK_SERIALIZATION_AND_DESERIALIZATION");
		}

		private static bool IsSpanSerializationAvailable(Compilation compilation)
		{
			var bitConverterSymbol = compilation.GetTypeByMetadataName("System.BitConverter");
			if (bitConverterSymbol != null)
			{
				var hasSingleToInt32Bits = bitConverterSymbol.GetMembers("SingleToInt32Bits")
					.OfType<IMethodSymbol>()
					.Any(method =>
						 method.Parameters.Length == 1 &&
						 method.Parameters[0].Type.SpecialType == SpecialType.System_Single &&
						 method.ReturnType.SpecialType == SpecialType.System_Int32);
				return hasSingleToInt32Bits;
			}
			return false;
		}

		private void AddSource(SourceProductionContext context, string fileName, CompilationUnitSyntax syntax)
		{
			var sourceText = MavlinkGeneratorConstants.AutoGeneratedHeader + "\n" + syntax.ToNormalizedString();
			context.AddSource($"{fileName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
		}

		private void AddSource(SourceProductionContext context, string fileName, string sourceText)
		{
			sourceText = MavlinkGeneratorConstants.AutoGeneratedHeader + "\n" + sourceText;
			context.AddSource($"{fileName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
		}
	}
}
