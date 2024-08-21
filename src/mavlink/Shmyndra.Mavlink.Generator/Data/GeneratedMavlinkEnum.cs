using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a generated Mavlink enum with additional metadata.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkEnumGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkEnum : MavlinkEnum
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

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkEnum"/> class.
	/// </summary>
	/// <param name="namespace">The namespace associated with the generated enum.</param>
	/// <param name="generatedName">The name of the generated enum.</param>
	/// <param name="generatedEntries">The array of generated entries.</param>
	/// <param name="declarationSyntax">The syntax declaration of the generated enum.</param>
	/// <param name="original">The original Mavlink enum.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/>, <paramref name="generatedName"/>, or <paramref name="declarationSyntax"/> is null.</exception>
	internal GeneratedMavlinkEnum(
		string @namespace,
		string generatedName,
		ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries,
		EnumDeclarationSyntax declarationSyntax,
		MavlinkEnum original)
		: base(original)
	{
		Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
		GeneratedEntries = generatedEntries;
		DeclarationSyntax = declarationSyntax ?? throw new ArgumentNullException(nameof(declarationSyntax));
	}
}
