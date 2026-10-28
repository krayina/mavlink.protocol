using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Mavlink.Protocol.Generator;

public partial class MavlinkEnumGenerator : IMavlinkEnumGenerator
{
	private static readonly Template _enumTemplate;

	private readonly Dictionary<(string Namespace, string Name), GeneratedMavlinkEnum> _generatedEnums = new();

	#region Explicit IGeneratedStorage Implementation

	ImmutableArray<GeneratedMavlinkEnum> IGeneratedStorage<GeneratedMavlinkEnum>.GetGeneratedTypes()
	{
		return _generatedEnums.Values.ToImmutableArray();
	}

	ImmutableArray<GeneratedMavlinkEnum> IGeneratedStorage<GeneratedMavlinkEnum>.GetGeneratedTypes(Func<GeneratedMavlinkEnum, bool>? predicate)
	{
		if (predicate == null)
		{
			return ((IGeneratedStorage<GeneratedMavlinkEnum>)this).GetGeneratedTypes();
		}

		return _generatedEnums.Values.Where(predicate).ToImmutableArray();
	}

	#endregion

	static MavlinkEnumGenerator()
	{
		_enumTemplate = Template.Parse(Templates.EnumTemplate);
		if (_enumTemplate.HasErrors)
		{
			var errors = string.Join("\n", _enumTemplate.Messages.Select(m => m.Message));
			throw new InvalidOperationException($"Failed to parse Enum Scriban template: \n{errors}");
		}
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

		var newEntries = BuildNewEnumEntries(@enum, normalizedName, @namespace);
		var existingEntries = existingEnums.SelectMany(BuildEntriesFromExisting).ToArray();
		var allEntries = existingEntries.Concat(newEntries).ToArray();

		var baseType = DetermineBaseTypeForAllEntries(@enum, existingEnums);
		var syntax = CreateFinalEnumDeclaration(@enum, normalizedName, baseType, allEntries);

		return new GeneratedMavlinkEnum(@namespace, normalizedName, baseType, allEntries.ToImmutableArray(), syntax, @enum);
	}

	private GeneratedMavlinkEnumEntry[] BuildNewEnumEntries(MavlinkEnum @enum, string enumName, string @namespace)
	{
		return @enum.Entries.OrderBy(e => e.Value).Select(entry =>
		{

			var normalizedName = Utilities.ToUpperCamelCase(entry.Name);
			var entryName = normalizedName == enumName ? "_" + normalizedName : normalizedName;
			var valueExpression = entry.Value.ToString();

			var equalsTokenWithSpaces = SyntaxFactory.Token(SyntaxKind.EqualsToken)
				.WithLeadingTrivia(SyntaxFactory.Space)
				.WithTrailingTrivia(SyntaxFactory.Space);

			var equalsClause = SyntaxFactory.EqualsValueClause(equalsTokenWithSpaces, SyntaxFactory.ParseExpression(valueExpression));

			var memberSyntax = SyntaxFactory.EnumMemberDeclaration(entryName)
				.WithEqualsValue(equalsClause);

			if (entry.Deprecated != null)
			{
				memberSyntax = memberSyntax.AddObsoleteAttribute(entry.Deprecated.ToString());
			}
			var leadingTrivia = new List<SyntaxTrivia>();
			if (!string.IsNullOrEmpty(entry.Description))
			{
				leadingTrivia.AddRange(Utilities.CreateSummaryTrivia(entry.Description!));
			}
			leadingTrivia.AddRange(Utilities.CreateRemarksTrivia($"Original name: {entry.Name}"));
			memberSyntax = memberSyntax.WithLeadingTrivia(leadingTrivia);

			return new GeneratedMavlinkEnumEntry(@namespace, entryName, memberSyntax, entry);
		}).ToArray();
	}

	private GeneratedMavlinkEnumEntry[] BuildEntriesFromExisting(GeneratedMavlinkEnum existing)
	{
		return existing.GeneratedEntries.Select(e =>
		{
			var valueExpression = $"{existing.Namespace}.{existing.GeneratedName}.{e.GeneratedName}";

			var equalsTokenWithSpaces = SyntaxFactory.Token(SyntaxKind.EqualsToken)
				.WithLeadingTrivia(SyntaxFactory.Space)
				.WithTrailingTrivia(SyntaxFactory.Space);

			var equalsClause = SyntaxFactory.EqualsValueClause(equalsTokenWithSpaces, SyntaxFactory.ParseExpression(valueExpression));

			var memberSyntax = SyntaxFactory.EnumMemberDeclaration(e.GeneratedName)
				.WithEqualsValue(equalsClause);

			if (e.Original.Deprecated != null)
			{
				memberSyntax = memberSyntax.AddObsoleteAttribute(e.Original.Deprecated.ToString());
			}

			var leadingTrivia = new List<SyntaxTrivia>();
			if (!string.IsNullOrEmpty(e.Original.Description))
			{
				leadingTrivia.AddRange(Utilities.CreateSummaryTrivia(e.Original.Description!));
			}
			leadingTrivia.AddRange(Utilities.CreateRemarksTrivia($"Original name: {e.Original.Name}"));

			memberSyntax = memberSyntax.WithLeadingTrivia(leadingTrivia);

			return new GeneratedMavlinkEnumEntry(existing.Namespace, e.GeneratedName, memberSyntax, e.Original);
		}).ToArray();
	}

	private string DetermineBaseTypeForAllEntries(MavlinkEnum newEnumData, ImmutableArray<GeneratedMavlinkEnum> existingEnums)
	{
		var allValues = newEnumData.Entries.Select(e => e.Value)
			.Concat(existingEnums.SelectMany(e => e.GeneratedEntries.Select(ge => ge.Original.Value)))
			.ToArray();
		return Utilities.DetermineEnumBaseType(allValues);
	}

	private EnumDeclarationSyntax CreateFinalEnumDeclaration(
		MavlinkEnum @enum,
		string enumName,
		string baseType,
		GeneratedMavlinkEnumEntry[] allEntries)
	{
		string? summaryCommentBlock = string.IsNullOrEmpty(@enum.Description)
			? null
			: Utilities.CreateSummaryTrivia(@enum.Description!).ToFullString().TrimEnd();

		var model = new EnumTemplateModel(
			summaryCommentBlock: summaryCommentBlock,
			remarks: $"Original name: {@enum.Name}",
			originalName: @enum.Name,
			isBitmask: @enum.Bitmask == true,
			isDeprecated: @enum.Deprecated != null,
			deprecatedReason: @enum.Deprecated?.ToString(),
			enumName: enumName,
			hasBaseType: baseType != "int",
			baseTypeName: baseType,
			entries: allEntries.Select(e => Utilities.IndentCode(e.DeclarationSyntax.ToFullString(), 1)).ToList()
		);
		var rendered = RenderTemplate(model);

		return SyntaxFactory.ParseMemberDeclaration(rendered) as EnumDeclarationSyntax
			?? throw new InvalidOperationException($"Failed to parse the generated enum '{enumName}'. Generated code:\n{rendered}");
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

	private string RenderTemplate(EnumTemplateModel model)
	{
		var context = CSharpScribanTemplateContext.Create();

		if (model.Entries.Count > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({model.Entries.Count}) exceeds LoopLimit ({context.LoopLimit}) for enum '{model.EnumName}'.");
		}

		var scriptObject = new ScriptObject();
		scriptObject.Import(model);

		context.PushGlobal(scriptObject);
		try
		{
			return _enumTemplate.Render(context).Trim();
		}
		finally
		{
			context.PopGlobal();
		}
	}
}
