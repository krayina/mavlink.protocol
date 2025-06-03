using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkEnumGenerator : IMavlinkEnumGenerator
{
	internal readonly struct ScribanEntryMetadata
	{
		public string ValueExpression { get; init; }
		public string? Summary { get; init; }
		public string Remarks { get; init; }
		public bool IsDeprecated { get; init; }
		public string? DeprecatedReason { get; init; }
	}

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
		var (newEntries, newMetadata) = BuildEnumEntries(@enum, normalizedName, @namespace);

		GeneratedMavlinkEnumEntry[] mergedEntries;
		Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata> mergedMetadata;
		uint[] allValues;
		if (existingEnums.IsEmpty)
		{
			mergedEntries = newEntries;
			mergedMetadata = newMetadata;
			allValues = @enum.Entries.Select(e => e.Value).ToArray();
		}
		else
		{
			var existingResults = existingEnums
				.Select(BuildExistingEntries)
				.ToArray();
			var existingEntries = existingResults
				.SelectMany(r => r.Item1)
				.ToArray();
			var existingMetadata = existingResults
				.Aggregate(new Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata>(),
					(acc, r) =>
					{
						foreach (var kvp in r.Item2)
						{
							acc[kvp.Key] = kvp.Value;
						}
						return acc;
					});

			mergedEntries = existingEntries.Concat(newEntries).ToArray();
			mergedMetadata = existingMetadata;
			foreach (var kvp in newMetadata)
			{
				mergedMetadata[kvp.Key] = kvp.Value;
			}
			allValues = existingEnums
				.SelectMany(e => e.GeneratedEntries.Select(ge => ge.Original.Value))
				.Concat(@enum.Entries.Select(e => e.Value))
				.ToArray();
		}

		var baseType = GetBaseType(allValues);
		var model = BuildScribanModel(@enum, normalizedName, baseType, mergedEntries, mergedMetadata);
		var syntax = RenderSyntax(model, normalizedName);
		var generatedEntries = mergedEntries.ToImmutableArray();

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

	private (GeneratedMavlinkEnumEntry[], Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata>) BuildEnumEntries(
		MavlinkEnum @enum, string enumName, string @namespace)
	{
		var entries = new List<GeneratedMavlinkEnumEntry>();
		var metadata = new Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata>();

		foreach (var entry in @enum.Entries.OrderBy(e => e.Value))
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

			var generatedEntry = new GeneratedMavlinkEnumEntry(
				@namespace,
				entryName,
				syntax,
				entry
			);

			var scribanMetadata = new ScribanEntryMetadata
			{
				ValueExpression = entry.Value.ToString(),
				Summary = entry.Description,
				Remarks = $"Original name: {entry.Name}",
				IsDeprecated = entry.Deprecated != null,
				DeprecatedReason = entry.Deprecated?.ToString()
			};

			entries.Add(generatedEntry);
			metadata[generatedEntry] = scribanMetadata;
		}

		return (entries.ToArray(), metadata);
	}

	private (GeneratedMavlinkEnumEntry[], Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata>) BuildExistingEntries(
		GeneratedMavlinkEnum existing)
	{
		var entries = new List<GeneratedMavlinkEnumEntry>();
		var metadata = new Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata>();

		foreach (var e in existing.GeneratedEntries)
		{
			var syntax = SyntaxFactory.EnumMemberDeclaration(e.GeneratedName)
				.WithEqualsValue(SyntaxFactory.EqualsValueClause(
					SyntaxFactory.ParseExpression($"{existing.Namespace}.{existing.GeneratedName}.{e.GeneratedName}")));

			var generatedEntry = new GeneratedMavlinkEnumEntry(
				existing.Namespace,
				e.GeneratedName,
				syntax,
				e.Original
			);

			var scribanMetadata = new ScribanEntryMetadata
			{
				ValueExpression = $"{existing.Namespace}.{existing.GeneratedName}.{e.GeneratedName}",
				Summary = e.Original.Description,
				Remarks = $"Original name: {e.Original.Name}",
				IsDeprecated = e.Original.Deprecated != null,
				DeprecatedReason = e.Original.Deprecated?.ToString()
			};

			entries.Add(generatedEntry);
			metadata[generatedEntry] = scribanMetadata;
		}

		return (entries.ToArray(), metadata);
	}

	private string GetBaseType(uint[] values)
	{
		return values.Any() ? Utilities.DetermineEnumBaseType(values) : "int";
	}

	private ScriptObject BuildScribanModel(
		MavlinkEnum @enum,
		string name,
		string baseType,
		GeneratedMavlinkEnumEntry[] entries,
		Dictionary<GeneratedMavlinkEnumEntry, ScribanEntryMetadata> metadata)
	{
		var scribanEntries = entries.Select(e => new
		{
			name = e.GeneratedName,
			value_expression = metadata[e].ValueExpression,
			summary = metadata[e].Summary,
			remarks = metadata[e].Remarks,
			is_deprecated = metadata[e].IsDeprecated,
			deprecated_reason = metadata[e].DeprecatedReason
		}).ToArray();

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
			["entries"] = new ScriptArray(scribanEntries)
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
}
