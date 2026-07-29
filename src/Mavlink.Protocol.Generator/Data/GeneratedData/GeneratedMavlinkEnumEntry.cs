using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mavlink.Protocol.Generator;

public record GeneratedMavlinkEnumEntry
{
	/// <summary>
	/// The namespace of the original Mavlink enum to which this entry belongs.
	/// </summary>
	/// <remarks>
	/// This property is used to indicate the namespace of the original enum from which this entry was generated.
	/// This is particularly important when merging enums from different namespaces into a single <see cref="GeneratedMavlinkEnum"/>,
	/// as it helps to track the origin of each entry.
	/// </remarks>
	public string Namespace { get; init; }

	/// <summary>
	/// The generated name of the Mavlink enum entry.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// The syntax node representing the enum member declaration in the generated C# code.
	/// </summary>
	public EnumMemberDeclarationSyntax DeclarationSyntax { get; init; }

	public MavlinkEnumEntry Original { get; init; }

	internal GeneratedMavlinkEnumEntry(
		string @namespace,
		string generatedName,
		EnumMemberDeclarationSyntax declarationSyntax,
		MavlinkEnumEntry original)
	{
		Namespace = @namespace;
		GeneratedName = generatedName;
		DeclarationSyntax = declarationSyntax;
		Original = original;
	}
}
