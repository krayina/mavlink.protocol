using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public interface IMavlinkEnumTypesGenerator
{
	List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping);
}

public class MavlinkEnumTypesGenerator : IMavlinkEnumTypesGenerator
{
	private readonly HashSet<string> _generatedEnumNames = new();

	public List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping)
	{
		var nameMappingDict = new Dictionary<string, (string Namespace, string TypeName)>();
		var enumDeclarations = new List<EnumDeclarationSyntax>();

		foreach (var enumData in enums)
		{
			if (_generatedEnumNames.Contains(enumData.Name))
			{
				continue;
			}

			var enumDeclaration = CreateEnum(enumData);
			enumDeclarations.Add(enumDeclaration);
			nameMappingDict[enumData.Name] = (namespaceName, enumDeclaration.Identifier.Text);
			_generatedEnumNames.Add(enumData.Name);
		}

		nameMapping = nameMappingDict.ToImmutableSortedDictionary();
		return enumDeclarations;
	}

	private EnumDeclarationSyntax CreateEnum((string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries) enumData)
	{
		var normalizedName = Utilities.ToCamelCase(enumData.Name);
		var allValues = enumData.Entries.Select(entry => ulong.Parse(entry.Value)).ToList();
		string enumBaseType = Utilities.GetEnumBaseType(allValues);

		var sortedEntries = enumData.Entries.OrderBy(entry => ulong.Parse(entry.Value)).ToList();
		var enumMembers = sortedEntries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == normalizedName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value)))
				.AddSummaryTriviaIfNotNull(entry.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name.ToUpper()}");

			return enumMember;
		});

		var enumDeclaration = SyntaxFactory.EnumDeclaration(normalizedName)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName(nameof(MavlinkTypes.MavlinkTypeAttribute)[0..^9]))
						.WithArgumentList(
							SyntaxFactory.AttributeArgumentList(
								SyntaxFactory.SeparatedList(new[]
								{
									SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
										SyntaxKind.StringLiteralExpression,
										SyntaxFactory.Literal(enumData.Name))
									)
								})
							)
						)
					)
				)
			)
			.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(enumMembers))
			.AddSummaryTriviaIfNotNull(enumData.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {enumData.Name.ToUpper()}");

		if (enumBaseType != "int")
		{
			enumDeclaration = enumDeclaration.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
				SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(enumBaseType)))));
		}

		return enumDeclaration;
	}
}
