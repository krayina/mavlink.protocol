using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

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
	/// Generates and caches a new MAVLink enum based on the provided data.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate.</param>
	/// <param name="namespace">The target namespace for the generated enum.</param>
	/// <returns>The generated MAVLink enum as a <see cref="GeneratedMavlinkEnum"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enum"/> or <paramref name="namespace"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when an enum with the same name already exists in the specified namespace, or when the enum data is invalid and cannot be processed.</exception>
	public GeneratedMavlinkEnum GenerateMavlinkEnum(MavlinkEnum @enum, string @namespace)
	{
		ValidateInput(@enum, @namespace);

		var key = (@namespace, @enum.Name);
		if (_generatedEnums.ContainsKey(key))
		{
			throw new InvalidOperationException($"Enum '{@enum.Name}' already exists in namespace '{@namespace}'.");
		}

		var result = GenerateEnum(@enum, @namespace, ImmutableArray<GeneratedMavlinkEnum>.Empty);
		_generatedEnums[key] = result;
		return result;
	}

	/// <summary>
	/// Generates a new MAVLink enum, merges it with the specified existing enums, and caches the result.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate.</param>
	/// <param name="namespace">The target namespace for the generated enum.</param>
	/// <param name="existingEnums">An immutable array of existing enums to merge with. Must contain at least one enum.</param>
	/// <returns>The generated and merged MAVLink enum as a <see cref="GeneratedMavlinkEnum"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enum"/>, <paramref name="namespace"/>, or <paramref name="existingEnums"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="existingEnums"/> is empty, when an enum with the same name already exists in the specified namespace, or when the enum data or merged entries are invalid and cannot be processed.</exception>
	public GeneratedMavlinkEnum GenerateAndMergeMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		params ImmutableArray<GeneratedMavlinkEnum> existingEnums)
	{
		ValidateInput(@enum, @namespace, existingEnums);

		var key = (@namespace, @enum.Name);
		if (_generatedEnums.ContainsKey(key))
		{
			throw new InvalidOperationException($"Enum '{@enum.Name}' already exists in namespace '{@namespace}'.");
		}

		if (existingEnums.IsEmpty)
		{
			throw new InvalidOperationException("At least one existing enum must be provided for merging.");
		}

		if (existingEnums.Any(e => e.Original.Name != @enum.Name))
		{
			throw new InvalidOperationException($"All enums to merge must have the same name as '{@enum.Name}'.");
		}

		var result = GenerateEnum(@enum, @namespace, existingEnums);
		_generatedEnums[key] = result;
		return result;
	}

	private GeneratedMavlinkEnum GenerateEnum(
		MavlinkEnum @enum,
		string @namespace,
		ImmutableArray<GeneratedMavlinkEnum> existingEnums)
	{
		var normalizedName = Utilities.ToUpperCamelCase(@enum.Name);
		var newEntries = BuildEnumEntries(@enum, normalizedName);

		object[] mergedEntries;
		uint[] allValues;
		if (existingEnums.IsEmpty)
		{
			mergedEntries = newEntries;
			allValues = @enum.Entries.Select(e => e.Value).ToArray();
		}
		else
		{
			var existingEntries = existingEnums
				.SelectMany(BuildExistingEntries)
				.ToArray();
			mergedEntries = existingEntries.Concat(newEntries).ToArray();
			allValues = existingEnums
				.SelectMany(e => e.GeneratedEntries.Select(ge => ge.Original.Value))
				.Concat(@enum.Entries.Select(e => e.Value))
				.ToArray();
		}

		var baseType = GetBaseType(allValues);
		var model = BuildScribanModel(@enum, normalizedName, baseType, mergedEntries);
		var syntax = RenderSyntax(model, normalizedName);
		var generatedEntries = BuildGeneratedEntries(@namespace, mergedEntries);

		return new GeneratedMavlinkEnum(@namespace, normalizedName, baseType, generatedEntries, syntax, @enum);
	}

	private void ValidateInput(MavlinkEnum @enum, string @namespace)
	{
		if (@enum == null) throw new ArgumentNullException(nameof(@enum));
		if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));
	}

	private void ValidateInput(MavlinkEnum @enum, string @namespace, ImmutableArray<GeneratedMavlinkEnum> existingEnums)
	{
		if (@enum == null) throw new ArgumentNullException(nameof(@enum));
		if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));
		if (existingEnums == null) throw new ArgumentNullException(nameof(existingEnums));
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
		var normalizedName = Utilities.ToUpperCamelCase(entry.Name);
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
}
