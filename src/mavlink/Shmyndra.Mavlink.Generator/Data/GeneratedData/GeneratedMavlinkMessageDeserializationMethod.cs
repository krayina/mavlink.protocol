using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents the generated deserialization method for a Mavlink message.
/// </summary>
public record GeneratedMavlinkMessageDeserializationMethod
{
	/// <summary>
	/// Gets the namespace of the generated message.
	/// </summary>
	public string Namespace { get; init; }

	/// <summary>
	/// Gets the name of the generated message.
	/// </summary>
	public string MessageName { get; init; }

	/// <summary>
	/// Gets the immutable array of fields representing the Mavlink message.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkMessageField> Fields { get; init; }

	public MethodDeclarationSyntax MethodSyntax { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageDeserializationMethod"/> record.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message.</param>
	/// <param name="messageName">The name of the generated message.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	internal GeneratedMavlinkMessageDeserializationMethod(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields,
		MethodDeclarationSyntax methodSyntax)
	{
		Namespace = @namespace;
		MessageName = messageName;
		Fields = fields;
		MethodSyntax = methodSyntax;
	}
}
