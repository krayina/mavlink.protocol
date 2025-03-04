using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides common helper methods and logic for generating Mavlink message serialization methods.
/// </summary>
public abstract class MavlinkMessageSerializationMethodGeneratorBase
{
	/// <summary>
	/// Creates a <see cref="GeneratedMavlinkMessageSerializeMethod"/>
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>
	/// A <see cref="GeneratedMavlinkMessageSerializeMethod"/> containing both the SerializeWithoutExtensions and
	/// SerializeWithExtensions methods for the message.
	/// </returns>
	public abstract GeneratedMavlinkMessageSerializeMethod CreateSerializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Generates the SerializeWithoutExtensions method for messages without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated SerializeWithoutExtensions method.</returns>
	internal abstract MethodDeclarationSyntax CreateSerializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Generates the SerializeWithExtensions method for messages with optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated SerializeWithExtensions method.</returns>
	internal abstract MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Wraps the generated method body into a temporary class in order to produce a <see cref="MethodDeclarationSyntax"/>.
	/// </summary>
	/// <param name="methodName">The name of the generated method.</param>
	/// <param name="methodBody">The generated method body as a string.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	protected MethodDeclarationSyntax WrapMethod(string methodName, string methodBody)
	{
		var methodString = $@"
public byte[] {methodName}()
{{
    {methodBody}
}}";

		var classWrapper = $@"
public class TemporaryClass
{{
    {methodString}
}}";

		var syntaxTree = CSharpSyntaxTree.ParseText(classWrapper);
		var root = syntaxTree.GetRoot();
		var method = root.DescendantNodes()
						 .OfType<MethodDeclarationSyntax>()
						 .First(m => m.Identifier.Text == methodName);
		return method;
	}
}
