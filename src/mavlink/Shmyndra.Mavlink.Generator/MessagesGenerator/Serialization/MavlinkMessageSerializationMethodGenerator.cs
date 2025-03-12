using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates serialization methods for Mavlink messages, converting instances of generated message types into byte streams.
/// This class uses a provided serialization strategy to support different approaches, such as buffer-based or span-based serialization.
/// </summary>
/// <remarks>
/// The class separates the serialization of required fields from optional extension fields, ensuring efficient processing
/// and extensibility. It relies on an <see cref="IMavlinkSerializationGeneratorStrategy"/> to generate the actual serialization code.
/// </remarks>
public class MavlinkMessageSerializationMethodGenerator
{
	private const string SerializeWithExtensionsMethodName = "SerializeWithExtensions";
	private const string SerializeWithoutExtensionsMethodName = "SerializeWithoutExtensions";

	private readonly IMavlinkSerializationGeneratorStrategy _serializationStrategy;

	public MavlinkMessageSerializationMethodGenerator(IMavlinkSerializationGeneratorStrategy serializationStrategy)
	{
		_serializationStrategy = serializationStrategy;
	}

	/// <summary>
	/// Creates serialization methods for a Mavlink message, including both methods with and without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageSerializeMethod"/> containing the serialization methods.</returns>
	public GeneratedMavlinkMessageSerializeMethod CreateSerializeMethod(ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		MethodDeclarationSyntax serializeWithoutExtensionsMethod = CreateSerializeWithoutExtensionsMethodInternal(fields);
		MethodDeclarationSyntax? serializeWithExtensionsMethod = null;

		if (fields.Any(x => !x.Original.IsRequired))
		{
			serializeWithExtensionsMethod = CreateSerializeWithExtensionsMethodInternal(fields);
		}

		return new GeneratedMavlinkMessageSerializeMethod(fields, serializeWithoutExtensionsMethod, serializeWithExtensionsMethod);
	}

	internal MethodDeclarationSyntax CreateSerializeWithoutExtensionsMethodInternal(ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int offset = 0;

		_serializationStrategy.AppendBufferInitialization(methodBody, fields.CalculateMinSize());
		AppendNonExtensionFields(methodBody, fields, ref offset);
		_serializationStrategy.AppendReturnStatement(methodBody);

		return WrapMethod(SerializeWithoutExtensionsMethodName, methodBody.ToString());
	}

	internal MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int offset = 0;

		_serializationStrategy.AppendBufferInitialization(methodBody, fields.CalculateFinalSize());
		AppendNonExtensionFields(methodBody, fields, ref offset);
		AppendExtensionFields(methodBody, fields, ref offset);
		_serializationStrategy.AppendReturnStatement(methodBody);

		return WrapMethod(SerializeWithExtensionsMethodName, methodBody.ToString());
	}

	private void AppendNonExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset)
	{
		var (requiredFields, arrayFields) = fields.GetSortedFields();
		foreach (var field in requiredFields)
		{
			_serializationStrategy.AppendFieldSerialization(sb, field, ref offset);
		}
		foreach (var field in arrayFields)
		{
			_serializationStrategy.AppendFieldSerialization(sb, field, ref offset);
		}
	}

	private void AppendExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset)
	{
		foreach (var field in fields.Where(f => !f.Original.IsRequired))
		{
			_serializationStrategy.AppendFieldSerialization(sb, field, ref offset);
		}
	}

	private MethodDeclarationSyntax WrapMethod(string methodName, string methodBody)
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
		return syntaxTree.GetRoot()
						 .DescendantNodes()
						 .OfType<MethodDeclarationSyntax>()
						 .First(m => m.Identifier.Text == methodName);
	}
}
