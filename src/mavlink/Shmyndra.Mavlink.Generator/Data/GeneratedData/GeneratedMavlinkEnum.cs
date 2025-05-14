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
	/// Specifies the base type for the generated enum, such as <see cref="byte"/>, <see cref="int"/>, or other valid integral types.
	/// This property is nullable; it may be <c>null</c> if the enum is empty and no base type is specified.
	/// </summary>
	public string? GeneratedBaseType { get; init; }

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
		string? generatedBaseType,
		ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries,
		EnumDeclarationSyntax declarationSyntax,
		MavlinkEnum original)
	{
		Namespace = @namespace;
		GeneratedName = generatedName;
		GeneratedEntries = generatedEntries;
		GeneratedBaseType = generatedBaseType;
		DeclarationSyntax = declarationSyntax;
		Original = original;
	}
}
