using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink message field that has been generated with an additional generated name.
/// </summary>
public record GeneratedMavlinkMessageField
{
	/// <summary>
	/// The generated name of the field.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// The type of the generated field. This property maps to the .NET representation
	/// of the Mavlink message field type.
	/// </summary>
	public GeneratedMavlinkMessageFieldTypeBase GeneratedType { get; init; }

	/// <summary>
	/// The declaration syntax of the field in C# code.
	/// This syntax represents the C# code structure for the field's property in the generated class.
	/// </summary>
	public PropertyDeclarationSyntax DeclarationSyntax { get; init; }

	/// <summary>
	/// The original Mavlink message field from which this instance is derived.
	/// </summary>
	public MavlinkMessageField Original { get; init; }

	internal GeneratedMavlinkMessageField(
		string generatedName,
		GeneratedMavlinkMessageFieldTypeBase generatedFieldType,
		PropertyDeclarationSyntax declarationSyntax,
		MavlinkMessageField original)
	{
		GeneratedName = generatedName;
		GeneratedType = generatedFieldType;
		DeclarationSyntax = declarationSyntax;
		Original = original;
	}
}
