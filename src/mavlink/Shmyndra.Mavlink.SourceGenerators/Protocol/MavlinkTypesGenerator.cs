using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Generator]
public class MavlinkTypesGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context) { }
	public void Execute(GeneratorExecutionContext context)
	{
		new Generator().Generate(context);
	}

	class Generator
	{
		internal void Generate(GeneratorExecutionContext context)
		{
			var xmlAdditionalFiles = context.AdditionalFiles.Where(x => x.Path.Contains(".xml")).ToList();
			if (xmlAdditionalFiles.Count == 0)
			{
				return;
			}
			Dictionary<string, string> xmlFilesData = xmlAdditionalFiles
				.ToDictionary(x => x.Path, x => x.GetText()!.ToString());

			var rootNodes = MavlinkXmlHierarchyBuilder.Build(xmlFilesData);
			List<MemberDeclarationSyntax> generatedTypes = new List<MemberDeclarationSyntax>();

			// Traverse the tree and generate enums and classes
			foreach (var rootNode in rootNodes)
			{
				TraverseAndGenerateTypes(rootNode, xmlFilesData, generatedTypes);
			}

			SyntaxTree syntaxTree = SyntaxFactory.SyntaxTree(
				SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
				.AddMembers(generatedTypes.ToArray())
				.NormalizeWhitespace());

			string code = syntaxTree.ToString();

		}
		static void TraverseAndGenerateTypes(TreeNode<string> node, Dictionary<string, string> xmlFiles, List<MemberDeclarationSyntax> generatedTypes)
		{
			// Load XML content from the dictionary
			var xmlContent = xmlFiles[node.Data];
			var xmlDoc = XDocument.Parse(xmlContent);

			foreach (var enumElement in xmlDoc.Descendants("enum"))
			{
				generatedTypes.Add(GenerateEnum(enumElement));
			}

			foreach (var classElement in xmlDoc.Descendants("class"))
			{
				generatedTypes.Add(GenerateClass(classElement));
			}

			foreach (var child in node.Children)
			{
				TraverseAndGenerateTypes(child, xmlFiles, generatedTypes);
			}
		}

		static EnumDeclarationSyntax GenerateEnum(XElement enumElement)
		{
			string enumName = ToCamelCase(enumElement.Attribute("name").Value);

			EnumDeclarationSyntax enumDeclaration = SyntaxFactory.EnumDeclaration(enumName)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

			foreach (var entry in enumElement.Elements("entry"))
			{
				string entryName = ToCamelCase(entry.Attribute("name").Value);
				string entryValue = entry.Attribute("value").Value;

				EnumMemberDeclarationSyntax enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(
						SyntaxFactory.ParseExpression(entryValue)));

				enumDeclaration = enumDeclaration.AddMembers(enumMember);
			}

			return enumDeclaration;
		}

		static ClassDeclarationSyntax GenerateClass(XElement classElement)
		{
			string className = ToCamelCase(classElement.Attribute("name").Value);

			ClassDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration(className)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

			foreach (var fieldElement in classElement.Elements("field"))
			{
				string fieldName = ToCamelCase(fieldElement.Attribute("name").Value);
				string fieldType = fieldElement.Attribute("type").Value;

				FieldDeclarationSyntax fieldDeclaration = SyntaxFactory.FieldDeclaration(
						SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(fieldType))
							.AddVariables(SyntaxFactory.VariableDeclarator(fieldName)))
					.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

				classDeclaration = classDeclaration.AddMembers(fieldDeclaration);
			}

			return classDeclaration;
		}

		static string ToCamelCase(string input)
		{
			if (string.IsNullOrEmpty(input) || !char.IsUpper(input[0]))
			{
				return input;
			}

			input = input.Replace("_", " ").ToLower();
			return new CultureInfo("en-US", false).TextInfo.ToTitleCase(input).Replace(" ", "");
		}
	}
}
