using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkEnumGenerator : IGeneratedStorage<GeneratedMavlinkEnum>
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

public partial class MavlinkEnumGenerator : IMavlinkEnumGenerator
{
	private readonly Dictionary<(string Namespace, string Name), GeneratedMavlinkEnum> _generatedEnums = new();
	private readonly Dictionary<string, HashSet<string>> _namespaceIncludesMap = new();
	private readonly Dictionary<string, string> _fileNameToPathMap = new();

	ImmutableArray<GeneratedMavlinkEnum> IGeneratedStorage<GeneratedMavlinkEnum>.GetGeneratedTypes()
	{
		return _generatedEnums.Values.ToImmutableArray();
	}

	ImmutableArray<GeneratedMavlinkEnum> IGeneratedStorage<GeneratedMavlinkEnum>.GetGeneratedTypes(Func<(string Namespace, string Name), bool> predicate)
	{
		return _generatedEnums
			.Where(keyValuePair => predicate(keyValuePair.Key))
			.Select(keyValuePair => keyValuePair.Value)
			.ToImmutableArray();
	}

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
		var entries = @enum.Entries
			.OrderBy(entry => entry.Value)
			.Select(entry =>
			{
				var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
				var entryName = normalizedEntryName == normalizedEnumName ? "_" + normalizedEntryName : normalizedEntryName;

				var entrySyntax = SyntaxFactory.EnumMemberDeclaration(entryName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())))
					.AddSummaryTriviaIfNotNull(entry.Description) // Додаємо summary
					.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name}"); // Додаємо remarks

				if (entry.Deprecated != null)
				{
					var obsoleteMessage = entry.Deprecated.ToString();
					var attribute = SyntaxFactory.Attribute(
						SyntaxFactory.ParseName("Obsolete"),
						SyntaxFactory.AttributeArgumentList(
							SyntaxFactory.SeparatedList(new[]
							{
							SyntaxFactory.AttributeArgument(
								SyntaxFactory.LiteralExpression(
									SyntaxKind.StringLiteralExpression,
									SyntaxFactory.Literal(obsoleteMessage)))
							})));

					entrySyntax = entrySyntax.AddAttributeLists(
						SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] { attribute })));
				}

				return new
				{
					name = entryName,
					value_expression = entry.Value.ToString(),
					summary = entry.Description,
					remarks = $"Original name: {entry.Name}",
					is_deprecated = entry.Deprecated != null,
					deprecated_reason = entry.Deprecated?.ToString(),
					syntax = entrySyntax,
					original = entry
				};
			})
			.ToArray();

		var context = CSharpScribanTemplateContext.Create();
		if (entries.Length > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({entries.Length}) exceeds LoopLimit ({context.LoopLimit})");
		}

		var allValues = @enum.Entries.Select(e => e.Value).ToArray();
		string? baseType = allValues.Any() ? Utilities.DetermineEnumBaseType(allValues) : null;
		bool hasBaseType = baseType != null && baseType != "int";

		var model = new ScriptObject
		{
			["summary"] = @enum.Description,
			["remarks"] = $"Original name: {@enum.Name}",
			["is_bitmask"] = @enum.Bitmask == true,
			["original_name"] = @enum.Name,
			["is_deprecated"] = @enum.Deprecated != null,
			["deprecated_reason"] = @enum.Deprecated?.ToString(),
			["enum_name"] = normalizedEnumName,
			["has_base_type"] = hasBaseType,
			["base_type_name"] = baseType,
			["entries"] = new ScriptArray(entries)
		};

		var template = Template.Parse(Templates.EnumTemplate);
		context.PushGlobal(model);
		try
		{
			string rendered = template.Render(context).Trim();
			var syntax = SyntaxFactory.ParseMemberDeclaration(rendered) as EnumDeclarationSyntax;
			if (syntax == null)
			{
				throw new InvalidOperationException($"Failed to parse the generated enum '{normalizedEnumName}'. Generated code:\n{rendered}");
			}
			var generatedEntries = entries.Select(e => new GeneratedMavlinkEnumEntry(
				namespaceName,
				e.name,
				e.syntax,
				e.original
			)).ToImmutableArray();
			return new GeneratedMavlinkEnum(namespaceName, normalizedEnumName, baseType, generatedEntries, syntax, @enum);
		}
		finally
		{
			context.PopGlobal();
		}
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
		var existingEntries = existingEnum.GeneratedEntries
			.Select(entry =>
			{
				var entryName = entry.GeneratedName;
				var valueExpression = $"{existingEnum.Namespace}.{existingEnum.GeneratedName}.{entryName}";
				var entrySyntax = SyntaxFactory.EnumMemberDeclaration(entryName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(valueExpression)));
				return new
				{
					name = entryName,
					value_expression = valueExpression,
					summary = entry.Original.Description,
					remarks = $"Original name: {entry.Original.Name}",
					is_deprecated = entry.Original.Deprecated != null,
					deprecated_reason = entry.Original.Deprecated?.ToString(),
					syntax = entrySyntax,
					original = entry.Original
				};
			})
			.ToList();

		var newEntries = newEnumData.Entries
			.Select(entry =>
			{
				var normalizedEntryName = Utilities.ToCamelCase(entry.Name);
				var entryName = normalizedEntryName == existingEnum.GeneratedName ? "_" + normalizedEntryName : normalizedEntryName;
				var entrySyntax = SyntaxFactory.EnumMemberDeclaration(entryName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())));
				return new
				{
					name = entryName,
					value_expression = entry.Value.ToString(),
					summary = entry.Description,
					remarks = $"Original name: {entry.Name}",
					is_deprecated = entry.Deprecated != null,
					deprecated_reason = entry.Deprecated?.ToString(),
					syntax = entrySyntax,
					original = entry
				};
			})
			.ToList();

		var mergedEntries = existingEntries.Concat(newEntries).ToArray();

		var context = CSharpScribanTemplateContext.Create();
		if (mergedEntries.Length > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({mergedEntries.Length}) exceeds LoopLimit ({context.LoopLimit})");
		}

		var allValues = existingEnum.GeneratedEntries.Select(e => e.Original.Value)
			.Concat(newEnumData.Entries.Select(e => e.Value))
			.ToArray();
		string newBaseType = allValues.Any() ? Utilities.DetermineEnumBaseType(allValues) : "int";
		bool hasBaseType = newBaseType != "int";

		var model = new ScriptObject
		{
			["summary"] = newEnumData.Description,
			["remarks"] = $"Original name: {newEnumData.Name}",
			["is_bitmask"] = newEnumData.Bitmask == true,
			["original_name"] = newEnumData.Name,
			["is_deprecated"] = newEnumData.Deprecated != null,
			["deprecated_reason"] = newEnumData.Deprecated?.ToString(),
			["enum_name"] = existingEnum.GeneratedName,
			["has_base_type"] = hasBaseType,
			["base_type_name"] = newBaseType,
			["entries"] = new ScriptArray(mergedEntries)
		};

		var template = Template.Parse(Templates.EnumTemplate);
		context.PushGlobal(model);
		try
		{
			string rendered = template.Render(context).Trim();
			var syntax = SyntaxFactory.ParseMemberDeclaration(rendered) as EnumDeclarationSyntax;
			if (syntax == null)
			{
				throw new InvalidOperationException($"Failed to parse the generated enum '{existingEnum.GeneratedName}'. Generated code:\n{rendered}");
			}
			var generatedEntries = mergedEntries.Select(e => new GeneratedMavlinkEnumEntry(
				existingNamespace,
				e.name,
				e.syntax,
				e.original
			)).ToImmutableArray();
			return new GeneratedMavlinkEnum(existingNamespace, existingEnum.GeneratedName, newBaseType, generatedEntries, syntax, newEnumData);
		}
		finally
		{
			context.PopGlobal();
		}
	}
}
