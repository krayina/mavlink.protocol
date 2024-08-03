using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkEnumGenerator
{
	/// <summary>
	/// Generates Mavlink enum and maps their names to namespaces and type names.
	/// </summary>
	/// <param name="enum">The Mavlink enum to be generated.</param>
	/// <param name="namespace">The namespace in which the generated enum will be placed.</param>
	/// <param name="includes">A list of included files that may contain existing enums to merge with.</param>
	/// <param name="filePath">The file path where the generated enum will be saved.</param>
	/// <returns>The generated Mavlink enum.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/> or <paramref name="includes"/> is <c>null</c>.</exception>
	/// <remarks>
	/// This method generates enums based on the provided data, merges with existing enums if necessary,
	/// and maps the generated enum names to their respective namespaces and type names.
	/// The resulting enums are represented as <see cref="GeneratedMavlinkEnum"/> instances.
	/// This method also initializes instances of <see cref="GeneratedMavlinkEnumEntry"/> within <see cref="GeneratedMavlinkEnum"/>.
	/// </remarks>
	GeneratedMavlinkEnum GenerateMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		ImmutableArray<string> includes,
		string filePath);
}

public class MavlinkEnumGenerator : IMavlinkEnumGenerator
{
	private readonly Dictionary<(string Namespace, string Name), GeneratedMavlinkEnum> _generatedEnums = new();
	private readonly Dictionary<string, HashSet<string>> _namespaceIncludesMap = new();
	private readonly Dictionary<string, string> _fileNameToPathMap = new();

	public GeneratedMavlinkEnum GenerateMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		ImmutableArray<string> includes,
		string filePath)
	{
		_fileNameToPathMap[Path.GetFileName(filePath)] = @namespace;

		var key = (Namespace: @namespace, @enum.Name);
		if (_generatedEnums.ContainsKey(key))
		{
			throw new InvalidOperationException($"Enum '{@enum.Name}' already exists in namespace '{@namespace}'.");
		}

		GeneratedMavlinkEnum? existingGeneratedEnum = null;

		// Check for existing enums in the current namespace or includes
		foreach (var include in includes)
		{
			if (_fileNameToPathMap.TryGetValue(include, out var includeNamespace))
			{
				var includeKey = (Namespace: includeNamespace, @enum.Name);
				if (_generatedEnums.TryGetValue(includeKey, out var includeGeneratedEnum))
				{
					if (existingGeneratedEnum is null)
					{
						existingGeneratedEnum = includeGeneratedEnum;
					}
					else
					{
						existingGeneratedEnum = GenerateAndMergeMavlinkEnumInternal(existingGeneratedEnum, @enum, includeNamespace);
					}
				}
			}
		}

		GeneratedMavlinkEnum generatedEnum;
		if (existingGeneratedEnum is null)
		{
			generatedEnum = GenerateMavlinkEnumInternal(@enum, @namespace);
		}
		else
		{
			generatedEnum = GenerateAndMergeMavlinkEnumInternal(existingGeneratedEnum, @enum, @namespace);
		}

		// Store the generated enum directly in _generatedEnums
		_generatedEnums[key] = generatedEnum;

		if (!_namespaceIncludesMap.ContainsKey(@enum.Name))
		{
			_namespaceIncludesMap[@enum.Name] = new HashSet<string>();
		}
		foreach (var include in includes)
		{
			_namespaceIncludesMap[@enum.Name].Add(include);
		}
		return generatedEnum;
	}

	/// <summary>
	/// Generates a Mavlink enum and its associated C# declaration syntax without caching.
	/// </summary>
	/// <param name="enum">The Mavlink enum data to generate.</param>
	/// <param name="namespaceName">The namespace in which the generated enum will reside.</param>
	/// <returns>A <see cref="GeneratedMavlinkEnum"/> containing the generated entries and declaration syntax.</returns>
	/// <remarks>
	/// This method creates the necessary syntax for the Mavlink enum based on the provided data.
	/// The resulting <see cref="GeneratedMavlinkEnum"/> includes the generated entries and their declaration syntax,
	/// but this method does not add the generated enum to the cache.
	/// </remarks>
	internal GeneratedMavlinkEnum GenerateMavlinkEnumInternal(
		MavlinkEnum @enum,
		string namespaceName)
	{
		var normalizedEnumName = Utilities.ToCamelCase(@enum.Name);
		ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries = [];

		var sortedEntries = @enum.Entries.OrderBy(entry => entry.Value);
		if (sortedEntries is not null)
		{
			generatedEntries = GenerateEnumMembersInternal(sortedEntries, normalizedEnumName, namespaceName);
		}

		var allValues = @enum.Entries.Select(entry => entry.Value);
		var enumBaseType = Utilities.DetermineEnumBaseType(allValues);

		var enumDeclaration = SyntaxFactory.EnumDeclaration(normalizedEnumName)
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
									SyntaxFactory.Literal(@enum.Name))
								)
								})
							)
						)
					)
				)
			)
			.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(generatedEntries.Select(entry => entry.DeclarationSyntax)))
			.AddSummaryTriviaIfNotNull(@enum.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {@enum.Name.ToUpper()}");

		if (enumBaseType != "int")
		{
			enumDeclaration = enumDeclaration.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
				SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(enumBaseType)))));
		}

		if (@enum.Bitmask == true)
		{
			enumDeclaration = enumDeclaration.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.FlagsAttribute"))
					)
				)
			);
		}

		return new GeneratedMavlinkEnum(namespaceName, normalizedEnumName, generatedEntries, enumDeclaration, @enum);
	}

	/// <summary>
	/// Generates and merges a new Mavlink enum with an existing generated enum.
	/// </summary>
	/// <param name="existingEnum">The existing generated Mavlink enum to merge with.</param>
	/// <param name="newEnumData">The new Mavlink enum data to merge.</param>
	/// <param name="existingNamespace">The namespace of the existing generated enum.</param>
	/// <returns>The merged generated Mavlink enum.</returns>
	/// <remarks>
	/// This method combines the entries from an existing generated enum and a new Mavlink enum, creating a new merged enum.
	/// It updates existing entries to reference their full namespace, creates new entries from the new enum data, and determines
	/// the appropriate base type for the merged enum. If the enum is a bitmask, it also adds the Flags attribute.
	/// </remarks>
	internal GeneratedMavlinkEnum GenerateAndMergeMavlinkEnumInternal(
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
		var newEntries = GenerateEnumMembersInternal(newEnumData.Entries, newEnumData.Name, existingNamespace).ToList();

		// Determine the maximum value among new entries
		var maxNewValue = newEnumData.Entries.Max(e => e.Value);

		// Determine the base type for the existing enum
		var currentBaseType = GetBaseType(existingEnum.DeclarationSyntax);

		var existingValues = new List<uint>();

		// Collect existing enum values
		foreach (var entry in existingEnum.GeneratedEntries)
		{
			var parsedValue = TryParseEnumValue(entry.DeclarationSyntax.EqualsValue!.Value);
			existingValues.Add(parsedValue);
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

	internal ImmutableArray<GeneratedMavlinkEnumEntry> GenerateEnumMembersInternal(
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
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())))
				.AddObsoleteAttribute(entry.Deprecated?.ToString());

			return new GeneratedMavlinkEnumEntry(enumNamespace, entryName, (EnumMemberDeclarationSyntax)enumMemberSyntax, entry);
		}).ToImmutableArray();
	}

	private static string GetBaseType(EnumDeclarationSyntax enumDeclaration)
	{
		return enumDeclaration.BaseList?.Types.FirstOrDefault()?.ToString() ?? "int";
	}

	private static uint TryParseEnumValue(ExpressionSyntax expression)
	{
		if (expression is LiteralExpressionSyntax literalExpression &&
			uint.TryParse(literalExpression.Token.ValueText, out var value))
		{
			return value;
		}
		// Handle other cases or return null if the value cannot be parsed
		throw new NotImplementedException($"Unsupported mavlink value {expression}");
	}
}
