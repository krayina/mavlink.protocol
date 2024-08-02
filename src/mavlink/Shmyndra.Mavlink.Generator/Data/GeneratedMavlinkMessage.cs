using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a generated Mavlink message with additional generated fields and namespace.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkMessageTypesGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkMessage : MavlinkMessage
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

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessage"/> record.
	/// </summary>
	/// <param name="generatedNamespace">The namespace associated with the generated message.</param>
	/// <param name="generatedName">The generated name of the Mavlink message.</param>
	/// <param name="generatedFields">The list of generated fields for the message.</param>
	/// <param name="declarationSyntax">The syntax declaration of the generated message.</param>
	/// <param name="originalMessage">The original Mavlink message.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedNamespace"/>, <paramref name="generatedName"/>, or <paramref name="generatedFields"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkMessage(
		string generatedNamespace,
		string generatedName,
		ImmutableArray<GeneratedMavlinkMessageField> generatedFields,
		RecordDeclarationSyntax declarationSyntax,
		MavlinkMessage originalMessage)
		: base(originalMessage)
	{
		GeneratedNamespace = generatedNamespace ?? throw new ArgumentNullException(nameof(generatedNamespace));
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
		GeneratedFields = generatedFields.IsDefault ? throw new ArgumentNullException(nameof(generatedFields)) : generatedFields;
		DeclarationSyntax = declarationSyntax ?? throw new ArgumentNullException(nameof(declarationSyntax));
	}
}
