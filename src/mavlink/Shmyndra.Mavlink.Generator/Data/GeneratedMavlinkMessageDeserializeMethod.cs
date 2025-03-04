using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator.Data;

/// <summary>
/// Represents the generated deserialization methods for a Mavlink message.
/// </summary>
public record GeneratedMavlinkMessageDeserializeMethod
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

	/// <summary>
	/// Gets the generated DeserializeWithoutExtensions method.
	/// </summary>
	public MethodDeclarationSyntax DeserializeWithoutExtensionsMethod { get; init; }

	/// <summary>
	/// Gets the generated DeserializeWithExtensions method, if available.
	/// </summary>
	public MethodDeclarationSyntax? DeserializeWithExtensionsMethod { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageDeserializeMethod"/> record.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message.</param>
	/// <param name="messageName">The name of the generated message.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="deserializeWithoutExtensionsMethod">The generated DeserializeWithoutExtensions method.</param>
	/// <param name="deserializeWithExtensionsMethod">The generated DeserializeWithExtensions method, if available.</param>
	internal GeneratedMavlinkMessageDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields,
		MethodDeclarationSyntax deserializeWithoutExtensionsMethod, MethodDeclarationSyntax? deserializeWithExtensionsMethod)
	{
		Namespace = @namespace;
		MessageName = messageName;
		Fields = fields;
		DeserializeWithoutExtensionsMethod = deserializeWithoutExtensionsMethod;
		DeserializeWithExtensionsMethod = deserializeWithExtensionsMethod;
	}
}
