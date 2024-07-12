using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkEnumTypesGenerator
{
	List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping);
}

public class MavlinkEnumTypesGenerator : IMavlinkEnumTypesGenerator
{
	private readonly Dictionary<string, EnumDeclarationSyntax> _generatedEnums = new();
	private readonly Dictionary<string, string> _namespaceMap = new();

	public List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping)
	{
		var nameMappingDict = new Dictionary<string, (string Namespace, string TypeName)>();
		var enumDeclarations = new List<EnumDeclarationSyntax>();

		foreach (var enumData in enums)
		{
			if (_generatedEnums.TryGetValue(enumData.Name, out var existingEnum))
			{
				var mergedEnum = MergeEnums(existingEnum, enumData, namespaceName);
				_generatedEnums[enumData.Name] = mergedEnum;
				enumDeclarations.Add(mergedEnum);
			}
			else
			{
				var newEnum = CreateEnum(enumData, namespaceName);
				_generatedEnums[enumData.Name] = newEnum;
				_namespaceMap[enumData.Name] = namespaceName;
				enumDeclarations.Add(newEnum);
			}

			nameMappingDict[enumData.Name] = (namespaceName, _generatedEnums[enumData.Name].Identifier.Text);
		}

		nameMapping = nameMappingDict.ToImmutableSortedDictionary();
		return enumDeclarations;
	}

	private IEnumerable<EnumMemberDeclarationSyntax> CreateEnumMembers(
		ImmutableList<(string Name, string Value, string? Description)> entries,
		string baseEnumName)
	{
		return entries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == baseEnumName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.AddSummaryTriviaIfNotNull(entry.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name.ToUpper()}")
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value)));

			return enumMember;
		});
	}

	private EnumDeclarationSyntax CreateEnum(
		(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries) enumData,
		string namespaceName)
	{
		var normalizedName = Utilities.ToCamelCase(enumData.Name);
		var allValues = enumData.Entries
			.Where(entry => entry.Value != null)
			.Select(entry => ulong.Parse(entry.Value!))
			.ToList();

		if (_generatedEnums.TryGetValue(enumData.Name, out var existingEnum))
		{
			var existingValues = existingEnum.Members
				.Where(m => m.EqualsValue != null)
				.Select(m => ulong.Parse(((LiteralExpressionSyntax)m.EqualsValue!.Value).Token.ValueText));
			allValues.AddRange(existingValues);
		}

		string enumBaseType = Utilities.DetermineEnumBaseType(allValues);

		var sortedEntries = enumData.Entries
			.Where(entry => entry.Value != null)
			.OrderBy(entry => ulong.Parse(entry.Value!))
			.ToList();

		var enumMembers = sortedEntries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == normalizedName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value!)))
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

	private EnumDeclarationSyntax MergeEnums(EnumDeclarationSyntax existingEnum, (string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries) newEnumData, string newNamespace)
	{
		if (!_namespaceMap.TryGetValue(existingEnum.Identifier.Text, out var existingNamespace))
		{
			existingNamespace = newNamespace;
		}

		var updatedExistingMembers = existingEnum.Members.Select(m =>
		{
			var newMember = SyntaxFactory.EnumMemberDeclaration(m.Identifier.Text)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression($"{existingNamespace}.{existingEnum.Identifier.Text}.{m.Identifier.Text}")));

			var leadingTrivia = m.GetLeadingTrivia();
			if (leadingTrivia.Any())
			{
				newMember = newMember.WithLeadingTrivia(leadingTrivia);
			}

			return newMember;
		}).ToList();

		var newMembers = CreateEnumMembers(newEnumData.Entries, newEnumData.Name).ToList();

		var maxNewValue = newEnumData.Entries.Max(e => ulong.Parse(e.Value));

		string currentBaseType = GetBaseType(existingEnum);

		var existingValues = new List<ulong>();

		foreach (var member in existingEnum.Members)
		{
			if (member.EqualsValue != null)
			{
				var parsedValue = TryParseEnumValue(member.EqualsValue.Value);
				if (parsedValue.HasValue)
				{
					existingValues.Add(parsedValue.Value);
				}
			}
		}

		existingValues.Add(maxNewValue);

		string newBaseType = Utilities.DetermineEnumBaseType(existingValues);

		var mergedMembers = updatedExistingMembers.Concat(newMembers).ToArray();

		var enumDeclaration = existingEnum.WithMembers(SyntaxFactory.SeparatedList(mergedMembers));

		if (newBaseType != currentBaseType)
		{
			enumDeclaration = enumDeclaration.WithBaseList(
				SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
					SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(newBaseType)))));
		}

		return enumDeclaration;
	}

	private static string GetBaseType(EnumDeclarationSyntax enumDeclaration)
	{
		return enumDeclaration.BaseList?.Types.FirstOrDefault()?.ToString() ?? "int";
	}

	private static ulong? TryParseEnumValue(ExpressionSyntax expression)
	{
		if (expression is LiteralExpressionSyntax literalExpression &&
			ulong.TryParse(literalExpression.Token.ValueText, out var value))
		{
			return value;
		}
		// Handle other cases or return null if the value cannot be parsed
		return null;
	}
}
