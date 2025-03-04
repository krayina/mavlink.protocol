using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An abstract base class responsible for generating serialization methods for Mavlink messages,
/// converting instances of generated message types into byte streams. This class provides a unified
/// framework for serializing fields, supporting different field types such as arrays, enums, and simple types.
/// </summary>
/// <remarks>
/// Derived classes must implement specific serialization logic for different field types, allowing for
/// flexibility in handling buffer-based or span-based serialization strategies. The class separates
/// the serialization of required fields from optional extension fields, ensuring efficient processing
/// and extensibility.
/// </remarks>
public abstract class MavlinkMessageSerializationMethodGeneratorBase
{
	protected const string SerializeWithExtensionsMethodName = "SerializeWithExtensions";
	protected const string SerializeWithoutExtensionsMethodName = "SerializeWithoutExtensions";

	/// <summary>
	/// Creates a serialization method for a Mavlink message, including both methods with and without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageSerializeMethod"/> containing the serialization methods.</returns>
	public GeneratedMavlinkMessageSerializeMethod CreateSerializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		MethodDeclarationSyntax serializeWithoutExtensionsMethod = CreateSerializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		MethodDeclarationSyntax? serializeWithExtensionsMethod = null;

		if (fields.Any(x => !x.IsRequired))
		{
			serializeWithExtensionsMethod = CreateSerializeWithExtensionsMethodInternal(@namespace, messageName, fields);
		}

		return new GeneratedMavlinkMessageSerializeMethod(@namespace, messageName, fields, serializeWithoutExtensionsMethod, serializeWithExtensionsMethod);
	}

	/// <summary>
	/// Generates the SerializeWithoutExtensions method for messages without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	internal MethodDeclarationSyntax CreateSerializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int minSize = fields.CalculateMinSize();
		int offset = 0;
		var (requiredFields, arrayFields) = fields.GetSortedFields();

		AppendMethodPrologue(methodBody, messageName, minSize);
		AppendNonExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendReturnStatement(methodBody);

		return WrapMethod(SerializeWithoutExtensionsMethodName, methodBody.ToString());
	}

	/// <summary>
	/// Generates the SerializeWithExtensions method for messages with optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	internal MethodDeclarationSyntax CreateSerializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int finalSize = fields.CalculateFinalSize();
		int offset = 0;

		AppendMethodPrologue(methodBody, messageName, finalSize);
		AppendNonExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendReturnStatement(methodBody);

		return WrapMethod(SerializeWithExtensionsMethodName, methodBody.ToString());
	}

	/// <summary>
	/// Appends the prologue for serialization, including buffer initialization logic.
	/// Derived classes must implement this method to provide type-specific prologue logic.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the prologue code to.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="requiredSize">The required size of the buffer.</param>
	protected abstract void AppendMethodPrologue(StringBuilder sb, string messageName, int requiredSize);

	/// <summary>
	/// Appends serialization logic for a single field.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information to serialize.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	/// <param name="isRequired">Indicates whether the field is required (non-optional).</param>
	protected virtual void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, bool isRequired)
	{
		string varName = Utilities.EscapeReservedKeyword(field.GeneratedName);

		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			AppendArrayFieldSerialization(sb, field, arrayType, ref offset, varName, isRequired);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType enumType)
		{
			AppendEnumFieldSerialization(sb, field, enumType, ref offset, varName, currentNamespace, isRequired);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType)
		{
			AppendArrayEnumFieldSerialization(sb, field, arrayEnumType, ref offset, varName, isRequired);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleType)
		{
			AppendSimpleFieldSerialization(sb, field, simpleType, ref offset, varName, isRequired);
		}
	}

	/// <summary>
	/// Appends serialization logic for simple fields (e.g., byte, int, float).
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing simple type details.</param>
	/// <param name="simpleType">The simple type information.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="isRequired">Indicates whether the field is required (non-optional).</param>
	protected abstract void AppendSimpleFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName, bool isRequired);

	/// <summary>
	/// Appends serialization logic for enum fields.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="enumType">The enum type information.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	/// <param name="isRequired">Indicates whether the field is required (non-optional).</param>
	protected abstract void AppendEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldEnumType enumType, ref int offset, string varName, string currentNamespace, bool isRequired);

	/// <summary>
	/// Appends serialization logic for array fields.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array type details.</param>
	/// <param name="arrayType">The array type information.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="isRequired">Indicates whether the field is required (non-optional).</param>
	protected abstract void AppendArrayFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName, bool isRequired);

	/// <summary>
	/// Appends serialization logic for array of enum fields.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="arrayEnumType">The array enum type information.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="isRequired">Indicates whether the field is required (non-optional).</param>
	protected abstract void AppendArrayEnumFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, ref int offset, string varName, bool isRequired);

	/// <summary>
	/// Appends serialization logic for non-extension fields (both required and array fields).
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected virtual void AppendNonExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string currentNamespace)
	{
		var (requiredFields, arrayFields) = fields.GetSortedFields();
		foreach (var field in requiredFields)
		{
			AppendFieldSerialization(sb, field, ref offset, currentNamespace, true);
		}
		foreach (var field in arrayFields)
		{
			AppendFieldSerialization(sb, field, ref offset, currentNamespace, true);
		}
	}

	/// <summary>
	/// Appends serialization logic for optional extension fields.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="offset">The current byte offset in the buffer, updated during serialization.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected virtual void AppendExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string currentNamespace)
	{
		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			AppendFieldSerialization(sb, field, ref offset, currentNamespace, false);
		}
	}

	/// <summary>
	/// Appends the return statement for the serialized buffer.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the return statement to.</param>
	protected virtual void AppendReturnStatement(StringBuilder sb)
	{
		sb.AppendLine("return buffer;");
	}

	/// <summary>
	/// Wraps the generated method body into a temporary class to produce a method declaration.
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
		return syntaxTree.GetRoot()
						 .DescendantNodes()
						 .OfType<MethodDeclarationSyntax>()
						 .First(m => m.Identifier.Text == methodName);
	}
}
