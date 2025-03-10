using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

public record GeneratedMavlinkEnum
{
	/// <summary>
	/// The namespace associated with the generated Mavlink enum.
	/// </summary>
	public string Namespace { get; init; }

	/// <summary>
	/// The name of the generated Mavlink enum.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// The array of generated entries in the Mavlink enum.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkEnumEntry> GeneratedEntries { get; init; }

	/// <summary>
	/// The declaration syntax of the generated enum.
	/// This syntax represents the C# code structure for the enum.
	/// </summary>
	public EnumDeclarationSyntax DeclarationSyntax { get; init; }

	public MavlinkEnum Original { get; init; }

	internal GeneratedMavlinkEnum(
		string @namespace,
		string generatedName,
		ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries,
		EnumDeclarationSyntax declarationSyntax,
		MavlinkEnum original)
	{
		Namespace = @namespace;
		GeneratedName = generatedName;
		GeneratedEntries = generatedEntries;
		DeclarationSyntax = declarationSyntax;
		Original = original;
	}
}
