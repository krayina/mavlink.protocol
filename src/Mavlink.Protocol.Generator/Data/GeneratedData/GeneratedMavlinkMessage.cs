using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mavlink.Protocol.Generator;

public record GeneratedMavlinkMessage
{
	/// <summary>
	/// The namespace associated with the generated Mavlink message.
	/// </summary>
	public string GeneratedNamespace { get; init; }

	/// <summary>
	/// The generated name of the Mavlink message.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// The list of generated fields in the Mavlink message.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkMessageField> GeneratedFields { get; init; }

	/// <summary>
	/// The declaration syntax of the generated message.
	/// This syntax represents the C# code structure for the message.
	/// </summary>
	public RecordDeclarationSyntax DeclarationSyntax { get; init; }

	public MavlinkMessage Original { get; init; }

	internal GeneratedMavlinkMessage(
		string generatedNamespace,
		string generatedName,
		ImmutableArray<GeneratedMavlinkMessageField> generatedFields,
		RecordDeclarationSyntax declarationSyntax,
		MavlinkMessage originalMessage)
	{
		GeneratedNamespace = generatedNamespace;
		GeneratedName = generatedName;
		GeneratedFields = generatedFields;
		DeclarationSyntax = declarationSyntax;
		Original = originalMessage;
	}
}
