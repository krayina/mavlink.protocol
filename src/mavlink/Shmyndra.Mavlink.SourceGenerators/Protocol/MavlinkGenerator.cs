using System.Collections.Immutable;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

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

			var enums = xmlFiles.SelectMany((files, _) => ParseEnums(files).ToImmutableArray());
			var messages = xmlFiles.SelectMany((files, _) => ParseMessages(files).ToImmutableArray());

			context.RegisterSourceOutput(enums.Collect(), GenerateEnumFile);
			context.RegisterSourceOutput(messages.Collect(), GenerateMessageFile);
		}

		private static IEnumerable<(string Name, string Description, List<(string Name, string Value, string Description)> Entries)> ParseEnums(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var e in mavlink.Enums)
				{
					yield return (ToCamelCase(e.Name), e.Description ?? "No description available", e.Entry.Select(entry => (ToCamelCase(entry.Name), entry.Value, entry.Description ?? "No description available")).ToList());
				}
			}
		}

		private static IEnumerable<(string Name, string Description, List<(string Type, string Name, string Description)> Fields)> ParseMessages(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var m in mavlink.Messages)
				{
					yield return (ToCamelCase(m.Name), m.Description ?? "No description available", m.Field.Select(field => (ConvertType(field.Type), ToCamelCase(field.Name), field.Description ?? "No description available")).ToList());
				}
			}
		}

		private static string ConvertType(string xmlType)
		{
			return xmlType switch
			{
				"uint8_t" => "byte",
				"int8_t" => "sbyte",
				"uint16_t" => "ushort",
				"int16_t" => "short",
				"uint32_t" => "uint",
				"int32_t" => "int",
				"uint64_t" => "ulong",
				"int64_t" => "long",
				"float" => "float",
				"double" => "double",
				_ => "object"
			};
		}

		private static void GenerateEnumFile(SourceProductionContext context, ImmutableArray<(string Name, string Description, List<(string Name, string Value, string Description)> Entries)> enums)
		{
			if (enums.IsDefaultOrEmpty)
				return;

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
				.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
					.AddMembers(enums.Select(CreateEnum).ToArray()));

			context.AddSource("MavlinkEnums.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
		}

		private static void GenerateMessageFile(SourceProductionContext context, ImmutableArray<(string Name, string Description, List<(string Type, string Name, string Description)> Fields)> messages)
		{
			if (messages.IsDefaultOrEmpty)
				return;

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
				.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
					.AddMembers(messages.Select(CreateRecordStruct).ToArray()));

			context.AddSource("MavlinkMessages.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
		}

		private static EnumDeclarationSyntax CreateEnum((string Name, string Description, List<(string Name, string Value, string Description)> Entries) enumData)
		{
			var enumMembers = enumData.Entries.Select(entry =>
				SyntaxFactory.EnumMemberDeclaration(entry.Name)
					.WithLeadingTrivia(CreateSummaryTrivia(entry.Description))
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value))));

			return SyntaxFactory.EnumDeclaration(enumData.Name)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(enumMembers))
				.WithLeadingTrivia(CreateSummaryTrivia(enumData.Description));
		}

		private static RecordDeclarationSyntax CreateRecordStruct((string Name, string Description, List<(string Type, string Name, string Description)> Fields) messageData)
		{
			var properties = messageData.Fields.Select(field =>
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(field.Type), field.Name)
					.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
					.AddAccessorListAccessors(
						SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
						SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
					.WithLeadingTrivia(CreateSummaryTrivia(field.Description)))
				.ToArray();

			return SyntaxFactory
				.RecordDeclaration(
					kind: SyntaxKind.RecordStructDeclaration,
					attributeLists: default,
					modifiers: default,
					keyword: SyntaxFactory.Token(SyntaxKind.RecordKeyword),
					classOrStructKeyword: SyntaxFactory.Token(SyntaxKind.StructKeyword),
					identifier: SyntaxFactory.Identifier(messageData.Name),
					typeParameterList: null,
					parameterList: null,
					baseList: null,
					constraintClauses: default,
					openBraceToken: SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
					members: SyntaxFactory.List<MemberDeclarationSyntax>(properties),
					closeBraceToken: SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
					semicolonToken: default)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				//.AddMembers(properties.ToArray())
				//.WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
				.WithLeadingTrivia(CreateSummaryTrivia(messageData.Description));
		}

		private static SyntaxTriviaList CreateSummaryTrivia(string description)
		{
			var summaryStart = SyntaxFactory.Comment("/// <summary>");
			var summaryContent = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
											.Select(line => SyntaxFactory.Comment($"/// {line.Trim()}"));
			var summaryEnd = SyntaxFactory.Comment("/// </summary>");

			return SyntaxFactory.TriviaList(summaryStart)
								.AddRange(summaryContent)
								.Add(summaryEnd);
		}

		private static string ToCamelCase(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < words.Length; i++)
			{
				words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
			}

			return string.Join("", words);
		}
	}
}
