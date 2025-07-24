using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.Generator;

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
					// Files that are not included in the project marked as "_"
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
				new MavlinkTreeBuilder(new MavlinkXmlParser()),
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

			// TODO: Should be resolver
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
							x.GeneratedType is GeneratedMavlinkMessageFieldEnumType or GeneratedMavlinkMessageFieldArrayEnumType)
				.Select(x => x.GeneratedType switch
				{
					GeneratedMavlinkMessageFieldEnumType enumType => (enumType.GeneratedEnum, enumType.ConvertedType),
					GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType => (arrayEnumType.GeneratedEnum, arrayEnumType.ConvertedType),
					_ => throw new NotImplementedException($"The type of {x.GeneratedType} is not recognized.")
				})
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

			IMavlinkMessageFieldTypeNameResolutionStrategy bitmaskTypeNameStrategy = useObjectiveBitmask
				? new MavlinkObjectiveBitmaskFieldTypeNameResolutionStrategy()
				: new MavlinkBitmaskFieldTypeNameResolutionStrategy();

			var typeNameResolver = new MavlinkFieldTypeNameResolverFacade(
				bitmaskTypeNameStrategy,
				new MavlinkNonBitmaskFieldTypeNameResolutionStrategy());

			IMavlinkSerializationGeneratorStrategy serializationStrategy;
			IMavlinkDeserializationGeneratorStrategy deserializationStrategy;

			if (supportsSpan)
			{
				IMavlinkFieldSerializationStrategy bitmaskSerializationStrategy = useObjectiveBitmask
					? new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy()
					: new BitmaskFieldSpanSerializationStrategy();

				IMavlinkFieldDeserializationStrategy bitmaskDeserializationStrategy = useObjectiveBitmask
					? new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy()
					: new MavlinkBitmaskFieldSpanDeserializationStrategy();

				serializationStrategy = new MavlinkSpanSerializationGeneratorStrategy(
					bitmaskSerializationStrategy,
					new NonBitmaskFieldSpanSerializationStrategy());

				deserializationStrategy = new MavlinkSpanDeserializationGeneratorStrategy(
					bitmaskDeserializationStrategy,
					new MavlinkNonBitmaskFieldSpanDeserializationStrategy());
			}
			else
			{
				IMavlinkFieldSerializationStrategy bitmaskSerializationStrategy = useObjectiveBitmask
					? new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy()
					: new MavlinkBitmaskFieldBufferSerializationStrategy();

				IMavlinkFieldDeserializationStrategy bitmaskDeserializationStrategy = useObjectiveBitmask
					? new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy()
					: new MavlinkBitmaskFieldBufferDeserializationStrategy();

				serializationStrategy = new MavlinkBufferSerializationGeneratorStrategy(
					bitmaskSerializationStrategy,
					new MavlinkNonBitmaskFieldBufferSerializationStrategy());

				deserializationStrategy = new MavlinkBufferDeserializationGeneratorStrategy(
					bitmaskDeserializationStrategy,
					new MavlinkNonBitmaskFieldBufferDeserializationStrategy());
			}

			return new MavlinkMessageGenerator(
				typeNameResolver,
				new MavlinkMessageDeserializationMethodGenerator(deserializationStrategy),
				new MavlinkMessageSerializationMethodGenerator(serializationStrategy)
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
