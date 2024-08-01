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
	private readonly Dictionary<(string Namespace, string Name), GeneratedMavlinkEnum> _generatedEnums = new();
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
			GeneratedMavlinkEnum? existingGeneratedEnum = null;

			// Check for existing enums in the current namespace or includes
			foreach (var include in includes)
			{
				if (_fileNameToPathMap.TryGetValue(include, out var includeNamespace))
				{
					var includeKey = (Namespace: includeNamespace, enumData.Name);
					if (_generatedEnums.TryGetValue(includeKey, out var includeGeneratedEnum))
					{
						if (existingGeneratedEnum is null)
						{
							existingGeneratedEnum = includeGeneratedEnum;
						}
						else
						{
							existingGeneratedEnum = MergeEnums(existingGeneratedEnum, enumData, includeNamespace);
						}
					}
				}
			}

			GeneratedMavlinkEnum finalEnum;
			if (existingGeneratedEnum is null)
			{
				finalEnum = CreateEnum(enumData, namespaceName);
			}
			else
			{
				finalEnum = MergeEnums(existingGeneratedEnum, enumData, namespaceName);
			}

			// Store the generated enum directly in _generatedEnums
			_generatedEnums[key] = finalEnum;
			enumDeclarations.Add(finalEnum.DeclarationSyntax);

			generatedTypesDict[enumData.Name] = finalEnum;

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

	private GeneratedMavlinkEnum CreateEnum(
		MavlinkEnum enumData,
		string namespaceName)
	{
		var enumName = Utilities.ToCamelCase(enumData.Name);
		ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries = [];

		var sortedEntries = enumData.Entries.OrderBy(entry => entry.Value);
		if (sortedEntries is not null)
		{
			generatedEntries = CreateEnumMembers(sortedEntries, enumName, namespaceName).ToImmutableArray();
		}

		var allValues = enumData.Entries.Select(entry => entry.Value);
		var enumBaseType = Utilities.DetermineEnumBaseType(allValues);

		var enumDeclaration = SyntaxFactory.EnumDeclaration(enumName)
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
			.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(generatedEntries.Select(entry => entry.DeclarationSyntax)))
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

		return new GeneratedMavlinkEnum(namespaceName, enumName, generatedEntries, enumDeclaration, enumData);
	}

	private GeneratedMavlinkEnum MergeEnums(
		GeneratedMavlinkEnum existingEnum,
		MavlinkEnum newEnumData,
		string existingNamespace)
	{
		// Update existing members with full namespace references
		var updatedExistingEntries = existingEnum.GeneratedEntries.Select(entry =>
		{
			var updatedDeclaration = entry.DeclarationSyntax.WithEqualsValue(
				SyntaxFactory.EqualsValueClause(
					SyntaxFactory.ParseExpression($"{entry.Namespace}.{existingEnum.GeneratedName}.{entry.GeneratedName}")
				));

			return entry with { DeclarationSyntax = updatedDeclaration };
		}).ToList();

		// Create new members from the new enum data
		var newEntries = CreateEnumMembers(newEnumData.Entries, newEnumData.Name, existingNamespace).ToList();

		// Determine the maximum value among new entries
		var maxNewValue = newEnumData.Entries.Max(e => e.Value);

		// Determine the base type for the existing enum
		var currentBaseType = GetBaseType(existingEnum.DeclarationSyntax);

		var existingValues = new List<uint>();

		// Collect existing enum values
		foreach (var entry in existingEnum.GeneratedEntries)
		{
			var parsedValue = TryParseEnumValue(entry.DeclarationSyntax.EqualsValue!.Value);
			if (parsedValue.HasValue)
			{
				existingValues.Add(parsedValue.Value);
			}
		}

		existingValues.Add(maxNewValue);

		// Determine the new base type considering all values
		var newBaseType = Utilities.DetermineEnumBaseType(existingValues);

		// Merge the entries
		var mergedEntries = updatedExistingEntries.Concat(newEntries).ToImmutableArray();

		// Create the final merged enum declaration
		var enumDeclaration = existingEnum.DeclarationSyntax.WithMembers(
			SyntaxFactory.SeparatedList(mergedEntries.Select(entry => entry.DeclarationSyntax))
		);

		// Adjust the base type if necessary
		if (newBaseType != currentBaseType)
		{
			enumDeclaration = enumDeclaration.WithBaseList(
				SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
					SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(newBaseType))))
			);
		}

		// Add Flags attribute if it's a bitmask
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

		// Create the merged GeneratedMavlinkEnum object
		return new GeneratedMavlinkEnum(
			existingEnum.Namespace,
			existingEnum.GeneratedName,
			mergedEntries,
			enumDeclaration,
			newEnumData
		);
	}

	private IEnumerable<GeneratedMavlinkEnumEntry> CreateEnumMembers(
		IEnumerable<MavlinkEnumEntry> entries,
		string enumName,
		string enumNamespace)
	{
		return entries.Select(entry =>
		{
			var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
			var entryName = normalizedEntryName == enumName ? "_" + normalizedEntryName : normalizedEntryName;

			var enumMemberSyntax = SyntaxFactory.EnumMemberDeclaration(entryName)
				.AddSummaryTriviaIfNotNull(entry.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name.ToUpper()}")
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())));

			return new GeneratedMavlinkEnumEntry(enumNamespace, entryName, enumMemberSyntax, entry);
		});
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
