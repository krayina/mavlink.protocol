using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkGenerator
{
	IImmutableDictionary<string, CompilationUnitSyntax> GenerateMavlink(IReadOnlyDictionary<string, string> mavlinkFileContents);
}

public class MavlinkGenerator
{
	private readonly IMavlinkFilesTreeBuilder _filesTreeBuilder;
	private readonly IMavlinkEnumGenerator _enumGenerator;
	private readonly IMavlinkMessageGenerator _messageGenerator;
	private readonly IMavlinkSpecificationGenerator _specificationGenerator;

	public MavlinkGenerator(
		IMavlinkFilesTreeBuilder filesTreeBuilder,
		IMavlinkEnumGenerator enumGenerator,
		IMavlinkMessageGenerator messageGenerator,
		IMavlinkSpecificationGenerator specificationGenerator)
	{
		_filesTreeBuilder = filesTreeBuilder;
		_enumGenerator = enumGenerator;
		_messageGenerator = messageGenerator;
		_specificationGenerator = specificationGenerator;
	}

	public IImmutableDictionary<string, CompilationUnitSyntax> GenerateMavlink(IReadOnlyDictionary<string, string> mavlinkFileContents)
	{
		Dictionary<string, CompilationUnitSyntax> generatedFileTypes = new Dictionary<string, CompilationUnitSyntax>();
		var fileTreeNods = _filesTreeBuilder.Build(mavlinkFileContents);

		fileTreeNods.ForEachTree(node =>
		{
			var filePath = node.FilePath;
			var data = node.Data;
			var namespaceName = $"{MavlinkGeneratorConstants.TypesNamespace}.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(filePath))}";
			List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

			// Generate enums
			foreach (var @enum in data.Enums)
			{
				var enumDeclarationSyntax = _enumGenerator.GenerateMavlinkEnum(@enum, namespaceName, data.Includes, filePath).DeclarationSyntax;
				members.Add(enumDeclarationSyntax);
			}

			// Generate messages
			foreach (var message in data.Messages)
			{
				Dictionary<string, GeneratedMavlinkEnum> dependedEnums = new();
				foreach (var field in message.Fields)
				{
					if (field.Type is MavlinkMessageFieldEnumType enumType)
					{
						GeneratedMavlinkEnum? generatedDependedEnum = null;
						var parentNode = fileTreeNods.GetParent(node);
						while (parentNode != null)
						{
							if (parentNode.Data.Enums.FirstOrDefault(x => x.Name == enumType.EnumName) != null)
							{
								var enumNamespace = $"{MavlinkGeneratorConstants.TypesNamespace}.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(parentNode.FilePath))}";
								generatedDependedEnum = _enumGenerator.GetGeneratedTypes(@enum => @enum.Namespace == enumNamespace && @enum.Name == enumType.EnumName).First();
								break;
							}
						}
						dependedEnums.Add(enumType.EnumName, generatedDependedEnum ?? throw new InvalidOperationException());
					}
				}

				var enumDeclarationSyntax = _messageGenerator.GenerateMavlinkMessage(message, namespaceName, dependedEnums.ToImmutableDictionary()).DeclarationSyntax;
				members.Add(enumDeclarationSyntax);
			}

			// Generated specification
			var specificationDeclarationSyntax = _specificationGenerator.GenerateSpecification(node.Data);
			members.Add(specificationDeclarationSyntax);

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddMembers(SyntaxFactory
					.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
					.AddMembers(members.ToArray())
				);
			generatedFileTypes.Add(filePath, compilationUnit);
		});

		return generatedFileTypes.ToImmutableDictionary();
	}
}
