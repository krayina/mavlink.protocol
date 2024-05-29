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

		public static IEnumerable<(string Name, string? Description, List<(string Name, string Value, string? Description)> Entries)> ParseEnums(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			var enumDict = new Dictionary<string, (string? Description, List<(string Name, string Value, string? Description)> Entries)>();

			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var e in mavlink.Enums)
				{
					var name = ToCamelCase(e.Name);
					var entries = e.Entry.Select(entry => (ToCamelCase(entry.Name), entry.Value, entry.Description)).ToList();

					if (enumDict.ContainsKey(name))
					{
						enumDict[name].Entries.AddRange(entries);
					}
					else
					{
						enumDict[name] = (e.Description, entries);
					}
				}
			}

			return enumDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Entries));
		}

		private static IEnumerable<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> ParseMessages(IEnumerable<string> xmlContents)
		{
			var serializer = new XmlSerializer(typeof(Mavlink));
			var messageDict = new Dictionary<string, (string? Description, List<(string Type, string Name, string? Description)> Fields)>();

			foreach (var xmlContent in xmlContents)
			{
				using var reader = new StringReader(xmlContent);
				var mavlink = (Mavlink)serializer.Deserialize(reader);
				foreach (var m in mavlink.Messages)
				{
					var name = ToCamelCase(m.Name);
					var fields = m.Field.Select(field => (ConvertType(field.Type), ToCamelCase(field.Name), field.Description)).ToList();

					if (messageDict.ContainsKey(name))
					{
						messageDict[name].Fields.AddRange(fields);
					}
					else
					{
						messageDict[name] = (m.Description, fields);
					}
				}
			}

			return messageDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Fields));
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

		public static void GenerateEnumFile(SourceProductionContext context, ImmutableArray<(string Name, string? Description, List<(string Name, string Value, string? Description)> Entries)> enums)
		{
			if (enums.IsDefaultOrEmpty)
				return;

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
				.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
					.AddMembers(enums.Select(CreateEnum).ToArray()));

			context.AddSource("MavlinkEnums.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
		}

		private static void GenerateMessageFile(SourceProductionContext context, ImmutableArray<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> messages)
		{
			if (messages.IsDefaultOrEmpty)
				return;

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
				.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
					.AddMembers(messages.Select(CreateRecordStruct).ToArray()));

			var code = compilationUnit.NormalizeWhitespace().ToFullString();
			context.AddSource("MavlinkMessages.g.cs", SourceText.From(code, Encoding.UTF8));
		}

		private static EnumDeclarationSyntax CreateEnum((string Name, string? Description, List<(string Name, string Value, string? Description)> Entries) enumData)
		{
			// Collect all values to determine the appropriate enum base type
			var allValues = enumData.Entries.Select(entry => ulong.Parse(entry.Value)).ToList();
			string enumBaseType = GetEnumBaseType(allValues);

			var enumMembers = enumData.Entries.Select(entry =>
			{
				var entryName = entry.Name == enumData.Name ? "_" + entry.Name : entry.Name;
				var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value)));
				return string.IsNullOrEmpty(entry.Description)
					? enumMember
					: enumMember.WithLeadingTrivia(CreateSummaryTrivia(entry.Description!));
			});

			var enumDeclaration = SyntaxFactory.EnumDeclaration(enumData.Name)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(enumMembers));

			if (enumBaseType != "int")
			{
				enumDeclaration = enumDeclaration.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
					SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(enumBaseType)))));
			}

			return string.IsNullOrEmpty(enumData.Description)
				? enumDeclaration
				: enumDeclaration.WithLeadingTrivia(CreateSummaryTrivia(enumData.Description!));
		}

		private static RecordDeclarationSyntax CreateRecordStruct((string Name, string? Description, List<(string Type, string Name, string? Description)> Fields) messageData)
		{
			var properties = messageData.Fields.Select(field =>
			{
				var fieldName = field.Name == messageData.Name ? "_" + field.Name : field.Name;
				var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(field.Type), fieldName)
					.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
					.AddAccessorListAccessors(
						SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
						SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
				return string.IsNullOrEmpty(field.Description)
					? property
					: property.WithLeadingTrivia(CreateSummaryTrivia(field.Description!));
			}).ToArray();

			var recordStruct = SyntaxFactory
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
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

			return string.IsNullOrEmpty(messageData.Description)
				? recordStruct
				: recordStruct.WithLeadingTrivia(CreateSummaryTrivia(messageData.Description!));
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

		public static string GetEnumBaseType(List<ulong> values)
		{
			var maxValue = values.Max();
			if (maxValue <= byte.MaxValue) return "byte";
			if (maxValue <= ushort.MaxValue) return "ushort";
			if (maxValue <= uint.MaxValue) return "uint";
			return "ulong";
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
