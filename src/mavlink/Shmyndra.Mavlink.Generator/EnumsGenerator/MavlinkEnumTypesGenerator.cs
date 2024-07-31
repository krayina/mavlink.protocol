using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkEnumTypesGenerator
{
	/// <summary>
	/// Generates Mavlink enums and maps their names to namespaces and type names.
	/// </summary>
	/// <param name="enums">The collection of Mavlink enums to be generated.</param>
	/// <param name="namespaceName">The namespace in which the generated enums will be placed.</param>
	/// <param name="includes">A list of included files that may contain existing enums to merge with.</param>
	/// <param name="filePath">The file path where the generated enums will be saved.</param>
	/// <param name="generatedTypes">An output parameter that maps enum names to their generated enum types.</param>
	/// <returns>A list of syntax nodes representing the generated enum declarations.</returns>
	/// <remarks>
	/// This method generates enums based on the provided data, merges with existing enums if necessary,
	/// and maps the generated enum names to their respective namespaces and type names. The resulting
	/// enums are represented as <see cref="GeneratedMavlinkEnum"/> instances. This method also initializes
	/// instances of <see cref="GeneratedMavlinkEnumEntry"/> within <see cref="GeneratedMavlinkEnum"/>.
	/// </remarks>
	List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<MavlinkEnum> enums,
		string namespaceName,
		ImmutableArray<string> includes,
		string filePath,
		out IImmutableDictionary<string, GeneratedMavlinkEnum> generatedTypes);
}

public class MavlinkEnumTypesGenerator : IMavlinkEnumTypesGenerator
{
	private readonly Dictionary<(string Namespace, string Name), EnumDeclarationSyntax> _generatedEnums = new();
	private readonly Dictionary<string, HashSet<string>> _namespaceIncludesMap = new();
	private readonly Dictionary<string, string> _fileNameToPathMap = new();

	public List<EnumDeclarationSyntax> GenerateEnums(
		ImmutableArray<MavlinkEnum> enums,
		string namespaceName,
		ImmutableArray<string> includes,
		string filePath,
		out IImmutableDictionary<string, GeneratedMavlinkEnum> generatedTypes)
	{
		var generatedTypesDict = new Dictionary<string, GeneratedMavlinkEnum>();
		var enumDeclarations = new List<EnumDeclarationSyntax>();

		_fileNameToPathMap[Path.GetFileName(filePath)] = namespaceName;

		foreach (var enumData in enums)
		{
			var key = (Namespace: namespaceName, enumData.Name);
			var existingEnums = new List<(EnumDeclarationSyntax Enum, string Namespace)>();

			// Check for existing enums in the current namespace or includes
			foreach (var include in includes)
			{
				if (_fileNameToPathMap.TryGetValue(include, out var includeNamespace))
				{
					var includeKey = (Namespace: includeNamespace, enumData.Name);
					if (_generatedEnums.TryGetValue(includeKey, out var includeEnum))
					{
						existingEnums.Add((includeEnum, includeNamespace));
					}
				}
			}

			EnumDeclarationSyntax? finalEnum = null;

			if (existingEnums.Count > 0)
			{
				// Merge all existing enums into one final enum
				foreach (var (existingEnum, includeNamespace) in existingEnums)
				{
					if (finalEnum is null)
					{
						finalEnum = MergeEnums(existingEnum, enumData, includeNamespace);
					}
					else
					{
						finalEnum = MergeEnums(finalEnum, enumData, namespaceName);
					}
				}
			}

			if (finalEnum is null)
			{
				finalEnum = CreateEnum(enumData, namespaceName);
			}

			_generatedEnums[key] = finalEnum;
			enumDeclarations.Add(finalEnum);

			var generatedEnumEntries = enumData.Entries
				.Select(entry => entry.ToGeneratedMavlinkEnumEntry(namespaceName, entry.Name))
				.ToImmutableArray();

			var generatedEnum = new GeneratedMavlinkEnum(namespaceName, generatedEnumEntries, enumData);
			generatedTypesDict[enumData.Name] = generatedEnum;

			if (!_namespaceIncludesMap.ContainsKey(enumData.Name))
			{
				_namespaceIncludesMap[enumData.Name] = new HashSet<string>();
			}
			foreach (var include in includes)
			{
				_namespaceIncludesMap[enumData.Name].Add(include);
			}
		}

		generatedTypes = generatedTypesDict.ToImmutableSortedDictionary();
		return enumDeclarations;
	}

	private IEnumerable<EnumMemberDeclarationSyntax> CreateEnumMembers(
		ImmutableArray<MavlinkEnumEntry> entries,
		string baseEnumName)
	{
		return entries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == baseEnumName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.AddSummaryTriviaIfNotNull(entry.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name.ToUpper()}")
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())));
			return enumMember;
		});
	}

	private EnumDeclarationSyntax CreateEnum(
		MavlinkEnum enumData,
		string namespaceName)
	{
		var normalizedName = Utilities.ToCamelCase(enumData.Name);

		var allValues = enumData.Entries.Select(entry => entry.Value);

		var enumBaseType = Utilities.DetermineEnumBaseType(allValues);

		var sortedEntries = enumData.Entries.OrderBy(entry => entry.Value);

		var enumMembers = sortedEntries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == normalizedName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMember = SyntaxFactory.EnumMemberDeclaration(entryName)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())))
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

		if (enumData.Bitmask == true)
		{
			enumDeclaration = enumDeclaration.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.FlagsAttribute"))
					)
				)
			);
		}

		return enumDeclaration;
	}

	private EnumDeclarationSyntax MergeEnums(EnumDeclarationSyntax existingEnum, MavlinkEnum newEnumData, string existingNamespace)
	{
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

		var maxNewValue = newEnumData.Entries.Max(e => e.Value);

		var currentBaseType = GetBaseType(existingEnum);

		var existingValues = new List<uint>();

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

		var newBaseType = Utilities.DetermineEnumBaseType(existingValues);

		var mergedMembers = updatedExistingMembers.Concat(newMembers).ToArray();

		var enumDeclaration = existingEnum.WithMembers(SyntaxFactory.SeparatedList(mergedMembers));

		if (newBaseType != currentBaseType)
		{
			enumDeclaration = enumDeclaration.WithBaseList(
				SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
					SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(newBaseType)))));
		}

		if (newEnumData.Bitmask == true)
		{
			enumDeclaration = enumDeclaration.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.FlagsAttribute"))
					)
				)
			);
		}

		return enumDeclaration;
	}

	private static string GetBaseType(EnumDeclarationSyntax enumDeclaration)
	{
		return enumDeclaration.BaseList?.Types.FirstOrDefault()?.ToString() ?? "int";
	}

	private static uint? TryParseEnumValue(ExpressionSyntax expression)
	{
		if (expression is LiteralExpressionSyntax literalExpression &&
			uint.TryParse(literalExpression.Token.ValueText, out var value))
		{
			return value;
		}
		// Handle other cases or return null if the value cannot be parsed
		return null;
	}
}
