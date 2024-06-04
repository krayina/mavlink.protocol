using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal static class EnumProcessor
{
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
				var entries = e.Entry.Select(entry => (entry.Name, entry.Value, entry.Description)).ToList();

				if (enumDict.ContainsKey(e.Name))
				{
					enumDict[e.Name].Entries.AddRange(entries);
				}
				else
				{
					enumDict[e.Name] = (e.Description, entries);
				}
			}
		}

		return enumDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Entries));
	}

	public static void GenerateEnumFile(SourceProductionContext context, ImmutableArray<(string Name, string? Description, List<(string Name, string Value, string? Description)> Entries)> enums)
	{
		if (enums.IsDefaultOrEmpty)
		{
			return;
		}

		var compilationUnit = SyntaxFactory.CompilationUnit()
			.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
				.AddMembers(enums.Select(CreateEnum).ToArray()));

		context.AddSource("MavlinkEnums.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
	}

	private static EnumDeclarationSyntax CreateEnum((string Name, string? Description, List<(string Name, string Value, string? Description)> Entries) enumData)
	{
		var normalizedName = Utilities.ToCamelCase(enumData.Name);

		// Collect all values to determine the appropriate enum base type
		var allValues = enumData.Entries.Select(entry => ulong.Parse(entry.Value)).ToList();
		string enumBaseType = Utilities.GetEnumBaseType(allValues);

		var enumMembers = enumData.Entries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == normalizedName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value)))
				.AddSummaryTriviaIfNotNull(entry.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original type: {entry.Name.ToUpper()}");

			return enumMember;
		});

		var enumDeclaration = SyntaxFactory.EnumDeclaration(normalizedName)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(
							SyntaxFactory.ParseName("Shmyndra.Mavlink.SourceGenerators.Protocol.MavlinkType")))))
			.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(enumMembers))
			.AddSummaryTriviaIfNotNull(enumData.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original type: {enumData.Name.ToUpper()}");

		if (enumBaseType != "int")
		{
			enumDeclaration = enumDeclaration.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
				SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(enumBaseType)))));
		}

		return enumDeclaration;
	}
}
