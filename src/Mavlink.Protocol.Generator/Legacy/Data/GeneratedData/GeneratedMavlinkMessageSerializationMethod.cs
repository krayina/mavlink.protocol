using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents the generated serialization method for a MAVLink message.
/// Each instance represents a single serialization method (either with or without extensions).
/// </summary>
public record GeneratedMavlinkMessageSerializationMethod
{
	/// <summary>
	/// Gets the namespace of the generated message.
	/// </summary>
	public string Namespace { get; }

	/// <summary>
	/// Gets the name of the generated message class.
	/// </summary>
	public string MessageName { get; }

	/// <summary>
	/// Gets the immutable array of fields that this method serializes.
	/// This is important to distinguish between methods with and without extension fields.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkMessageField> Fields { get; }

	/// <summary>
	/// Gets the Roslyn syntax tree for the generated method.
	/// </summary>
	public MethodDeclarationSyntax MethodSyntax { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageSerializationMethod"/> record.
	/// </summary>
	public GeneratedMavlinkMessageSerializationMethod(
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
