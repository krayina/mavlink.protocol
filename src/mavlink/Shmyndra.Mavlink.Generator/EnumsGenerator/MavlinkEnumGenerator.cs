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
	/// Generates a MAVLink enum or merges it with existing enums from included namespaces.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate or merge.</param>
	/// <param name="namespace">The target namespace where the generated enum will be placed.</param>
	/// <param name="includedNamespaces">An immutable array of namespaces containing existing enums to merge with.</param>
	/// <returns>The generated or merged MAVLink enum as a <see cref="GeneratedMavlinkEnum"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enum"/>, <paramref name="namespace"/>, or <paramref name="includedNamespaces"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when an enum with the same name already exists in the specified namespace.</exception>
	/// <remarks>
	/// This method generates a MAVLink enum based on the provided data, merging it with existing enums of the same name found in the specified included namespaces, if any.
	/// The resulting enum includes generated entries as <see cref="GeneratedMavlinkEnumEntry"/> instances and is cached for future reference.
	/// </remarks>
	GeneratedMavlinkEnum GenerateMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		ImmutableArray<string> includedNamespaces);
}

public partial class MavlinkEnumGenerator : IMavlinkEnumGenerator
{
	private readonly Dictionary<(string Namespace, string Name), GeneratedMavlinkEnum> _generatedEnums = new();

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

	/// <summary>
	/// Generates a new MAVLink enum or merges it with existing enums from specified included namespaces.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate or merge.</param>
	/// <param name="namespace">The target namespace where the enum will be generated.</param>
	/// <param name="includedNamespaces">An immutable array of namespaces to check for existing enums to merge with.</param>
	/// <returns>The newly generated or merged MAVLink enum.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="enum"/>, <paramref name="namespace"/>, or <paramref name="includedNamespaces"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if an enum with the same name already exists in the specified namespace, or if the number of enum entries exceeds the Scriban loop limit, or if the generated enum syntax cannot be parsed.</exception>
	public GeneratedMavlinkEnum GenerateMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		ImmutableArray<string> includedNamespaces)
	{
		ValidateInput(@enum, @namespace, includedNamespaces);

		var key = (@namespace, @enum.Name);
		if (_generatedEnums.ContainsKey(key))
		{
			throw new InvalidOperationException($"Enum '{@enum.Name}' already exists in namespace '{@namespace}'.");
		}

		var existingEnums = FindExistingEnums(@enum.Name, includedNamespaces);
		GeneratedMavlinkEnum result;

		if (existingEnums.Any())
		{
			var mergedEnum = existingEnums.Aggregate((acc, next) => MergeEnums(acc, next));
			result = GenerateAndMergeMavlinkEnumInternal(mergedEnum, @enum, @namespace);
		}
		else
		{
			result = GenerateMavlinkEnumInternal(@enum, @namespace);
		}

		_generatedEnums[key] = result;
		return result;
	}

	/// <summary>
	/// Generates a new MAVLink enum without merging or caching.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate.</param>
	/// <param name="namespaceName">The namespace in which the generated enum will reside.</param>
	/// <returns>A generated MAVLink enum containing the entries and declaration syntax.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the number of enum entries exceeds the Scriban loop limit, or if the generated enum syntax cannot be parsed.</exception>
	internal GeneratedMavlinkEnum GenerateMavlinkEnumInternal(
		MavlinkEnum @enum,
		string namespaceName)
	{
		var normalizedName = Utilities.ToCamelCase(@enum.Name);
		var entries = BuildEnumEntries(@enum, normalizedName);
		var baseType = GetBaseType(@enum.Entries.Select(e => e.Value).ToArray());
		var model = BuildScribanModel(@enum, normalizedName, baseType, entries);
		var syntax = RenderSyntax(model, normalizedName);
		var generatedEntries = BuildGeneratedEntries(namespaceName, entries);

		return new GeneratedMavlinkEnum(namespaceName, normalizedName, baseType, generatedEntries, syntax, @enum);
	}

	/// <summary>
	/// Generates and merges a new MAVLink enum with an existing generated enum.
	/// </summary>
	/// <param name="existingEnum">The existing generated MAVLink enum to merge with.</param>
	/// <param name="newEnumData">The new MAVLink enum data to merge.</param>
	/// <param name="existingNamespace">The namespace of the existing generated enum.</param>
	/// <returns>A new generated MAVLink enum containing merged entries.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the number of merged enum entries exceeds the Scriban loop limit, or if the generated enum syntax cannot be parsed.</exception>
	internal GeneratedMavlinkEnum GenerateAndMergeMavlinkEnumInternal(
		GeneratedMavlinkEnum existingEnum,
		MavlinkEnum newEnumData,
		string existingNamespace)
	{
		var existingEntries = BuildExistingEntries(existingEnum);
		var newEntries = BuildEnumEntries(newEnumData, existingEnum.GeneratedName);
		var mergedEntries = existingEntries.Concat(newEntries).ToArray();
		var allValues = existingEnum.GeneratedEntries.Select(e => e.Original.Value)
			.Concat(newEnumData.Entries.Select(e => e.Value))
			.ToArray();
		var baseType = GetBaseType(allValues);
		var model = BuildScribanModel(newEnumData, existingEnum.GeneratedName, baseType, mergedEntries);
		var syntax = RenderSyntax(model, existingEnum.GeneratedName);
		var generatedEntries = BuildGeneratedEntries(existingNamespace, mergedEntries);

		return new GeneratedMavlinkEnum(existingNamespace, existingEnum.GeneratedName, baseType, generatedEntries, syntax, newEnumData);
	}

	private void ValidateInput(MavlinkEnum @enum, string @namespace, ImmutableArray<string> includedNamespaces)
	{
		if (@enum == null) throw new ArgumentNullException(nameof(@enum));
		if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));
		if (includedNamespaces == null) throw new ArgumentNullException(nameof(includedNamespaces));
	}

	private List<GeneratedMavlinkEnum> FindExistingEnums(string enumName, ImmutableArray<string> includedNamespaces)
	{
		var result = new List<GeneratedMavlinkEnum>();
		foreach (var ns in includedNamespaces)
		{
			if (_generatedEnums.TryGetValue((ns, enumName), out var enumValue))
			{
				result.Add(enumValue);
			}
		}
		return result;
	}

	private object[] BuildEnumEntries(MavlinkEnum @enum, string enumName)
	{
		return @enum.Entries
			.OrderBy(e => e.Value)
			.Select(e => CreateEnumEntry(e, enumName))
			.ToArray();
	}

	private object CreateEnumEntry(MavlinkEnumEntry entry, string enumName)
	{
		var normalizedName = Utilities.ToCamelCase(entry.Name);
		var entryName = normalizedName == enumName ? "_" + normalizedName : normalizedName;
		var syntax = SyntaxFactory.EnumMemberDeclaration(entryName)
			.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(entry.Value.ToString())))
			.AddSummaryTriviaIfNotNull(entry.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {entry.Name}");

		if (entry.Deprecated != null)
		{
			syntax = syntax.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SeparatedList(
					[
						SyntaxFactory.Attribute(SyntaxFactory.ParseName("Obsolete"))
							.WithArgumentList(SyntaxFactory.AttributeArgumentList(
								SyntaxFactory.SeparatedList(
								[
									SyntaxFactory.AttributeArgument(
										SyntaxFactory.LiteralExpression(
											SyntaxKind.StringLiteralExpression,
											SyntaxFactory.Literal(entry.Deprecated.ToString())))
								])))
					])));
		}

		return new
		{
			name = entryName,
			value_expression = entry.Value.ToString(),
			summary = entry.Description,
			remarks = $"Original name: {entry.Name}",
			is_deprecated = entry.Deprecated != null,
			deprecated_reason = entry.Deprecated?.ToString(),
			syntax,
			original = entry
		};
	}

	private string GetBaseType(uint[] values)
	{
		return values.Any() ? Utilities.DetermineEnumBaseType(values) : "int";
	}

	private ScriptObject BuildScribanModel(MavlinkEnum @enum, string name, string baseType, object[] entries)
	{
		return new ScriptObject
		{
			["summary"] = @enum.Description,
			["remarks"] = $"Original name: {@enum.Name}",
			["is_bitmask"] = @enum.Bitmask == true,
			["original_name"] = @enum.Name,
			["is_deprecated"] = @enum.Deprecated != null,
			["deprecated_reason"] = @enum.Deprecated?.ToString(),
			["enum_name"] = name,
			["has_base_type"] = baseType != "int",
			["base_type_name"] = baseType,
			["entries"] = new ScriptArray(entries)
		};
	}

	private EnumDeclarationSyntax RenderSyntax(ScriptObject model, string enumName)
	{
		var context = CSharpScribanTemplateContext.Create();
		var entriesArray = (ScriptArray)model["entries"];
		if (entriesArray.Count > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({entriesArray.Count}) exceeds LoopLimit ({context.LoopLimit})");
		}

		var template = Template.Parse(Templates.EnumTemplate);
		context.PushGlobal(model);
		try
		{
			var rendered = template.Render(context).Trim();
			return SyntaxFactory.ParseMemberDeclaration(rendered) as EnumDeclarationSyntax
				?? throw new InvalidOperationException($"Failed to parse the generated enum '{enumName}'. Generated code:\n{rendered}");
		}
		finally
		{
			context.PopGlobal();
		}
	}

	private ImmutableArray<GeneratedMavlinkEnumEntry> BuildGeneratedEntries(string @namespace, object[] entries)
	{
		return entries.Select(e => new GeneratedMavlinkEnumEntry(
			@namespace,
			e.GetType().GetProperty("name").GetValue(e).ToString(),
			(EnumMemberDeclarationSyntax)e.GetType().GetProperty("syntax").GetValue(e),
			(MavlinkEnumEntry)e.GetType().GetProperty("original").GetValue(e)
		)).ToImmutableArray();
	}

	private object[] BuildExistingEntries(GeneratedMavlinkEnum existing)
	{
		return existing.GeneratedEntries
			.Select(e => new
			{
				name = e.GeneratedName,
				value_expression = $"{existing.Namespace}.{existing.GeneratedName}.{e.GeneratedName}",
				summary = e.Original.Description,
				remarks = $"Original name: {e.Original.Name}",
				is_deprecated = e.Original.Deprecated != null,
				deprecated_reason = e.Original.Deprecated?.ToString(),
				syntax = SyntaxFactory.EnumMemberDeclaration(e.GeneratedName)
					.WithEqualsValue(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression($"{existing.Namespace}.{existing.GeneratedName}.{e.GeneratedName}"))),
				original = e.Original
			})
			.ToArray();
	}

	private GeneratedMavlinkEnum MergeEnums(GeneratedMavlinkEnum enum1, GeneratedMavlinkEnum enum2)
	{
		var mergedEntries = enum1.GeneratedEntries.Concat(enum2.GeneratedEntries)
			.GroupBy(e => e.GeneratedName)
			.Select(g => g.First())
			.ToImmutableArray();

		var baseType = GetBaseType(mergedEntries.Select(e => e.Original.Value).ToArray());
		var syntax = enum1.DeclarationSyntax.WithMembers(
			SyntaxFactory.SeparatedList(mergedEntries.Select(e => e.DeclarationSyntax)));

		return new GeneratedMavlinkEnum(enum1.Namespace, enum1.GeneratedName, baseType, mergedEntries, syntax, enum1.Original);
	}
}
