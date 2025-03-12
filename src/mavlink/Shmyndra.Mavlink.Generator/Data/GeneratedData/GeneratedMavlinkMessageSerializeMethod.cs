using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents the generated serialization methods for a Mavlink message.
/// </summary>
public record GeneratedMavlinkMessageSerializeMethod
{
	/// <summary>
	/// Gets the immutable array of fields representing the Mavlink message.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkMessageField> Fields { get; init; }

	/// <summary>
	/// Gets the generated SerializeWithoutExtensions method.
	/// </summary>
	public MethodDeclarationSyntax SerializeWithoutExtensionsMethod { get; init; }

	/// <summary>
	/// Gets the generated SerializeWithExtensions method.
	/// </summary>
	public MethodDeclarationSyntax? SerializeWithExtensionsMethod { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageSerializeMethod"/> record.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message.</param>
	/// <param name="messageName">The name of the generated message.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="serializeWithoutExtensionsMethod">The generated SerializeWithoutExtensions method.</param>
	/// <param name="serializeWithExtensionsMethod">The generated SerializeWithExtensions method.</param>
	internal GeneratedMavlinkMessageSerializeMethod(
		ImmutableArray<GeneratedMavlinkMessageField> fields,
		MethodDeclarationSyntax serializeWithoutExtensionsMethod,
		MethodDeclarationSyntax? serializeWithExtensionsMethod)
	{
		Fields = fields;
		SerializeWithoutExtensionsMethod = serializeWithoutExtensionsMethod;
		SerializeWithExtensionsMethod = serializeWithExtensionsMethod;
	}
}
