using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.MavlinkGenerator;

[Generator]
public sealed class MavlinkIncrementalGenerator : IIncrementalGenerator, IDisposable
{
	private readonly AssemblyResolver _assemblyResolver;
	private readonly MavlinkMemberDefinition _contentGenerator;
	private bool _disposed;

	public MavlinkIncrementalGenerator()
		: this(new MavlinkEnumTypesGenerator(), new MavlinkMessageTypesGenerator(), new MavlinkSpecificationTypeGenerator())
	{
	}

	internal MavlinkIncrementalGenerator(
		IMavlinkEnumTypesGenerator enumGenerator,
		IMavlinkMessageTypesGenerator messageGenerator,
		IMavlinkSpecificationTypeGenerator specificationGenerator)
	{
		_contentGenerator = new MavlinkMemberDefinition(enumGenerator, messageGenerator, specificationGenerator);
		_assemblyResolver = new AssemblyResolver("System.ComponentModel.Annotations");
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			//Uncomment this line to enter Debug mode for the generator and perform a Rebuild
			//System.Diagnostics.Debugger.Launch();
		}
		RegisterSourceGeneration(context);
	}

	private void RegisterSourceGeneration(IncrementalGeneratorInitializationContext context)
	{
		var xmlFiles = context.AdditionalTextsProvider
			.Where(file => file.Path.EndsWith(".xml"))
			.Select((file, _) => (file.Path, Content: file.GetText()!.ToString()))
			.Collect();
		context.RegisterSourceOutput(xmlFiles, GenerateSourceFiles);
	}

	private void GenerateSourceFiles(SourceProductionContext sourceProductionContext, ImmutableArray<(string Path, string Content)> files)
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
				var members = _contentGenerator.GenerateNamespaceMembers(mavlinkData, namespaceName);
				AddSource(sourceProductionContext, namespaceName, members);
			}
		}
		catch (Exception ex)
		{
			sourceProductionContext.ReportDiagnostic(
				Diagnostic.Create(
					MavlinkGeneratorDiagnostics.GenericProtocolErrorRule,
					Location.None,
					ex.Message
				)
			);
		}
	}

	private void AddSource(SourceProductionContext context, string namespaceName, List<MemberDeclarationSyntax> members)
	{
		var compilationUnit = SyntaxFactory.CompilationUnit()
			.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
			.AddMembers(members.ToArray()));

		context.AddSource($"{namespaceName}.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_assemblyResolver?.Dispose();
			}
			_disposed = true;
		}
	}

	~MavlinkIncrementalGenerator()
	{
		Dispose(false);
	}
}
