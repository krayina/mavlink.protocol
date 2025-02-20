using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An abstract base class responsible for generating deserialization methods for Mavlink messages, converting byte streams into instances of the generated message types.
/// This class provides common logic for deserialization both with and without optional extension fields, supporting different field types such as arrays, enums, and simple types.
/// </summary>
/// <remarks>
/// Derived classes must implement the specific deserialization logic for different types of fields, including handling array deserialization, enum deserialization, and the deserialization of simple types.
/// This class is designed to be extended for different serialization strategies, such as buffer-based or span-based deserialization, ensuring flexibility in deserialization implementation.
/// </remarks>
public abstract class MavlinkMessageDeserializationMethodGeneratorBase
{
	/// <summary>
	/// The fully-qualified name of the ImmutableArray.CreateRange method used for creating immutable arrays.
	/// </summary>
	protected const string CreateRangeWithNamespace = "System.Collections.Immutable.ImmutableArray.CreateRange";
	protected const string DeserializeParameterName = "payload";

	/// <summary>
	/// Creates a deserialization method for a Mavlink message.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageDeserializeMethod"/> containing the deserialization methods.</returns>
	public abstract GeneratedMavlinkMessageDeserializeMethod CreateDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Generates the DeserializeWithoutExtensions method for messages without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated DeserializeWithoutExtensions method.</returns>
	internal abstract MethodDeclarationSyntax CreateDeserializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Generates the DeserializeWithExtensions method for messages with optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated DeserializeWithExtensions method.</returns>
	internal abstract MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Wraps the generated method body into a temporary class to produce a <see cref="MethodDeclarationSyntax"/> for a static method.
	/// </summary>
	/// <param name="methodName">The name of the generated method.</param>
	/// <param name="returnType">The return type of the generated method.</param>
	/// <param name="methodBody">The generated method body as a string.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	protected MethodDeclarationSyntax WrapMethod(string methodName, string returnType, string methodBody)
	{
		var methodString = $@"
public static {returnType} {methodName}(byte[] payload)
{{
    {methodBody}
}}";
		var classWrapper = $@"
public class TemporaryClass
{{
    {methodString}
}}";
		var syntaxTree = CSharpSyntaxTree.ParseText(classWrapper);
		return syntaxTree.GetRoot()
						  .DescendantNodes()
						  .OfType<MethodDeclarationSyntax>()
						  .First(m => m.Identifier.Text == methodName);
	}

	protected string GetVariableName(string generatedName)
	{
		var lower = char.ToLowerInvariant(generatedName[0]) + generatedName.Substring(1);
		if (lower == DeserializeParameterName)
		{
			return "_" + lower;
		}
		return Utilities.EscapeReservedKeyword(lower);
	}

	protected string GetCombinedTypeForTotalBits(int totalBits)
	{
		if (totalBits <= 8)
		{
			return "byte";
		}
		else if (totalBits <= 16)
		{
			return "ushort";
		}
		else if (totalBits <= 32)
		{
			return "uint";
		}
		else
		{
			return "ulong";
		}
	}
}
