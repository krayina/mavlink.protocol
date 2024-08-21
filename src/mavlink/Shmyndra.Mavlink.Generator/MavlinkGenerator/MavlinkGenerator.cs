using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkGenerator
{
	/// <summary>
	/// Generates code based on the content of MAVLink files.
	/// </summary>
	/// <param name="mavlinkFileContents">A collection of key-value pairs where the key is the file path and the value is the file content.</param>
	/// <returns>
	/// An immutable dictionary containing generated <see cref="CompilationUnitSyntax"/> objects, 
	/// where the key is the file path and the value is the generated code for the corresponding file.
	/// </returns>
	IImmutableDictionary<string, (string Namespace, CompilationUnitSyntax Syntax)> GenerateMavlink(IReadOnlyDictionary<string, string> mavlinkFileContents);
}

public class MavlinkGenerator : IMavlinkGenerator
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

	public IImmutableDictionary<string, (string Namespace, CompilationUnitSyntax Syntax)> GenerateMavlink(IReadOnlyDictionary<string, string> mavlinkFileContents)
	{
		var fileTreeNods = _filesTreeBuilder.Build(mavlinkFileContents);
		var generatedFiles = new Dictionary<string, (int Index, (string Namespace, CompilationUnitSyntax Syntax))>();

		int i = 0;
		fileTreeNods.ForEachTree(node =>
		{
			var members = new List<MemberDeclarationSyntax>();
			var namespaceName = GenerateNamespaceName(node.FilePath);

			GenerateEnums(node, namespaceName, members);

			GenerateMessages(node, fileTreeNods, namespaceName, members);

			GenerateSpecification(node, members);

			var compilationUnit = CreateCompilationUnit(namespaceName, members);
			generatedFiles.Add(node.FilePath, (i++, (namespaceName, compilationUnit)));
		});

		return generatedFiles.ToImmutableIndexSortedDictionary();
	}

	private string GenerateNamespaceName(string filePath)
	{
		return $"{MavlinkGeneratorConstants.TypesNamespace}.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(filePath))}";
	}

	private void GenerateEnums(MavlinkFileNode node, string namespaceName, List<MemberDeclarationSyntax> members)
	{
		foreach (var @enum in node.Data.Enums)
		{
			var enumDeclarationSyntax = _enumGenerator.GenerateMavlinkEnum(@enum, namespaceName, node.Data.Includes, node.FilePath).DeclarationSyntax;
			members.Add(enumDeclarationSyntax);
		}
	}

	private void GenerateMessages(MavlinkFileNode node, ReadOnlyMavlinkTree fileTreeNods, string namespaceName, List<MemberDeclarationSyntax> members)
	{
		foreach (var message in node.Data.Messages)
		{
			var dependedEnums = GetDependedEnumsForMessage(node, fileTreeNods, message);
			var messageDeclarationSyntax = _messageGenerator.GenerateMavlinkMessage(message, namespaceName, dependedEnums.ToImmutableDictionary()).DeclarationSyntax;
			members.Add(messageDeclarationSyntax);
		}
	}

	private Dictionary<string, GeneratedMavlinkEnum> GetDependedEnumsForMessage(MavlinkFileNode node, ReadOnlyMavlinkTree fileTreeNods, MavlinkMessage message)
	{
		Dictionary<string, GeneratedMavlinkEnum> dependedEnums = new();

		foreach (var field in message.Fields)
		{
			if (field.Type is MavlinkMessageFieldEnumType enumType
				&& !dependedEnums.ContainsKey(enumType.EnumName))
			{
				var nodeWithDependedEnum = node.FindNode(node => node.Data.Enums.FirstOrDefault(@enum => @enum.Name == enumType.EnumName) != null);

				if (nodeWithDependedEnum is not null)
				{
					var enumNamespace = $"{MavlinkGeneratorConstants.TypesNamespace}.{Utilities.ToCamelCase(Path.GetFileNameWithoutExtension(nodeWithDependedEnum.FilePath))}";
					GeneratedMavlinkEnum generatedDependedEnum = _enumGenerator.GetGeneratedTypes(@enum => @enum.Namespace == enumNamespace && @enum.Name == enumType.EnumName).First();
					dependedEnums.Add(enumType.EnumName, generatedDependedEnum);
				}
				else
				{
					throw new InvalidOperationException($"Enum '{enumType.EnumName}' not found in the hierarchy.");
				}
			}
		}

		return dependedEnums;
	}

	private void GenerateSpecification(MavlinkFileNode node, List<MemberDeclarationSyntax> members)
	{
		var specificationDeclarationSyntax = _specificationGenerator.GenerateSpecification(node.Data);
		members.Add(specificationDeclarationSyntax);
	}

	private CompilationUnitSyntax CreateCompilationUnit(string namespaceName, List<MemberDeclarationSyntax> members)
	{
		return SyntaxFactory.CompilationUnit()
			.AddMembers(SyntaxFactory
				.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
				.AddMembers(members.ToArray())
			);
	}
}
