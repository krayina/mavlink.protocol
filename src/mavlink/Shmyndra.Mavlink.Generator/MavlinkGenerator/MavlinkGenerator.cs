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
	private readonly IMavlinkTreeBuilder _filesTreeBuilder;
	private readonly IMavlinkEnumGenerator _enumGenerator;
	private readonly IMavlinkEnumTreeGenerator _enumTreeGenerator;
	private readonly IMavlinkMessageGenerator _messageGenerator;
	private readonly IMavlinkSpecificationGenerator _specificationGenerator;

	public MavlinkGenerator(
		IMavlinkTreeBuilder filesTreeBuilder,
		// TODO : Temporary object. Should be removed with new messages architecture
		IMavlinkEnumGenerator enumGenerator,
		IMavlinkEnumTreeGenerator enumTreeGenerator,
		IMavlinkMessageGenerator messageGenerator,
		IMavlinkSpecificationGenerator specificationGenerator)
	{
		_filesTreeBuilder = filesTreeBuilder;
		_enumGenerator = enumGenerator;
		_enumTreeGenerator = enumTreeGenerator;
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
			var namespaceName = GenerateNamespaceName(node.Namespace);

			var generatedEnums = _enumTreeGenerator.GenerateEnums(node, namespaceName);
			members.AddRange(generatedEnums.Select(e => e.DeclarationSyntax));

			GenerateMessages(node, fileTreeNods, namespaceName, members);

			GenerateSpecification(node, members);

			var compilationUnit = CreateCompilationUnit(namespaceName, members);
			generatedFiles.Add(node.Namespace, (i++, (namespaceName, compilationUnit)));
		});

		return generatedFiles.ToImmutableIndexSortedDictionary();
	}

	private string GenerateNamespaceName(string filePath)
	{
		return $"{MavlinkGeneratorConstants.TypesNamespace}.{Utilities.ToUpperCamelCase(Path.GetFileNameWithoutExtension(filePath))}";
	}

	private void GenerateMessages(MavlinkNode node, ReadOnlyMavlinkTree fileTreeNods, string namespaceName, List<MemberDeclarationSyntax> members)
	{
		foreach (var message in node.Data.Messages)
		{
			var dependedEnums = GetDependedEnumsForMessage(node, fileTreeNods, message);
			var messageDeclarationSyntax = _messageGenerator.GenerateMavlinkMessage(message, namespaceName, dependedEnums.ToImmutableArray()).DeclarationSyntax;
			members.Add(messageDeclarationSyntax);
		}
	}

	private List<GeneratedMavlinkEnum> GetDependedEnumsForMessage(MavlinkNode node, ReadOnlyMavlinkTree fileTreeNods, MavlinkMessage message)
	{
		var dependedEnums = new List<GeneratedMavlinkEnum>();
		var seenEnumNames = new HashSet<string>();

		foreach (var field in message.Fields)
		{
			if (field.Type is MavlinkMessageFieldEnumType enumType
				&& seenEnumNames.Add(enumType.EnumName))
			{
				var nodeWithDependedEnum = node.FindNode(n => n.Data.Enums.Any(e => e.Name == enumType.EnumName));

				if (nodeWithDependedEnum is not null)
				{
					var enumFileNamespace = Utilities.ToUpperCamelCase(Path.GetFileNameWithoutExtension(nodeWithDependedEnum.Namespace));
					var enumNamespace = $"{MavlinkGeneratorConstants.TypesNamespace}.{enumFileNamespace}";

					GeneratedMavlinkEnum? generatedDependedEnum = _enumGenerator
						.GetGeneratedTypes(
							e => e.Namespace == enumNamespace
							&& e.Original.Name == enumType.EnumName
						).FirstOrDefault();

					if (generatedDependedEnum is not null)
					{
						dependedEnums.Add(generatedDependedEnum);
					}
					else
					{
						throw new InvalidOperationException($"Enum '{enumType.EnumName}' was found in a node but could not be retrieved from the generator storage.");
					}
				}
				else
				{
					throw new InvalidOperationException($"Enum '{enumType.EnumName}' not found in the hierarchy.");
				}
			}
		}

		return dependedEnums;
	}

	private void GenerateSpecification(MavlinkNode node, List<MemberDeclarationSyntax> members)
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
