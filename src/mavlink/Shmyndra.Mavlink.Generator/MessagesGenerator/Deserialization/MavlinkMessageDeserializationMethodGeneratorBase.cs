using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;
using System.Collections.Immutable;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An abstract base class responsible for generating deserialization methods for Mavlink messages,
/// converting byte streams into instances of the generated message types.
/// This class provides common logic for deserialization both with and without optional extension fields,
/// supporting different field types such as arrays, enums, and simple types.
/// </summary>
/// <remarks>
/// Derived classes must implement the specific deserialization logic for different types of fields,
/// including handling array deserialization, enum deserialization, and the deserialization of simple types.
/// This class is designed to be extended for different serialization strategies, such as buffer-based or span-based deserialization,
/// ensuring flexibility in deserialization implementation.
/// </remarks>
public abstract class MavlinkMessageDeserializationMethodGeneratorBase
{
	/// <summary>
	/// The fully-qualified name of the ImmutableArray.CreateRange method used for creating immutable arrays.
	/// </summary>
	protected const string CreateRangeWithNamespace = "System.Collections.Immutable.ImmutableArray.CreateRange";

	/// <summary>
	/// The name of the parameter that represents the payload for deserialization.
	/// </summary>
	protected const string DeserializeParameterName = "payload";

	protected const string DeserializeWithExtensionsMethodName = "DeserializeWithExtensions";
	protected const string DeserializeWithoutExtensionsMethodName = "DeserializeWithoutExtensions";

	/// <summary>
	/// Creates a deserialization method for a Mavlink message.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>
	/// A <see cref="GeneratedMavlinkMessageDeserializeMethod"/> containing the deserialization methods for both 
	/// messages with and without optional extension fields.
	/// </returns>
	public virtual GeneratedMavlinkMessageDeserializeMethod CreateDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		MethodDeclarationSyntax deserializeWithoutExtensionsMethod = CreateDeserializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		MethodDeclarationSyntax? deserializeWithExtensionsMethod = null;

		if (fields.Any(x => !x.IsRequired))
		{
			deserializeWithExtensionsMethod = CreateDeserializeWithExtensionsMethodInternal(@namespace, messageName, fields);
		}

		return new GeneratedMavlinkMessageDeserializeMethod(@namespace, messageName, fields, deserializeWithoutExtensionsMethod, deserializeWithExtensionsMethod);
	}

	/// <summary>
	/// Generates the DeserializeWithoutExtensions method for messages without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>
	/// A <see cref="MethodDeclarationSyntax"/> representing the generated DeserializeWithoutExtensions method.
	/// </returns>
	internal abstract MethodDeclarationSyntax CreateDeserializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Generates the DeserializeWithExtensions method for messages with optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>
	/// A <see cref="MethodDeclarationSyntax"/> representing the generated DeserializeWithExtensions method.
	/// </returns>
	internal abstract MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields);

	/// <summary>
	/// Appends the deserialization logic for an array field to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="arrayType">The array field type information.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendArrayFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldArrayType arrayType, ref int offset, string varName);

	/// <summary>
	/// Appends the deserialization logic for an enum field to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends the deserialization logic for an array of enums to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendArrayEnumFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends the deserialization logic for a simple (primitive) field to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="simpleType">The simple field type information.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendSimpleFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageFieldType simpleType, ref int offset, string varName);

	/// <summary>
	/// Appends the assignment of deserialized values to the properties of the generated message type.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the assignment code to.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected void AppendAssignments(StringBuilder sb, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields, string currentNamespace)
	{
		var assignments = string.Join(",\n", fields.Select(field =>
		{
			var varName = GetVariableName(field.GeneratedName);
			if (field.Type is GeneratedMavlinkMessageFieldEnumType enumField)
			{
				if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
				{
					return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
				}
				else
				{
					string enumTypeName = GetQualifiedEnumTypeName(enumField, currentNamespace);
					return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}Enum";
				}
			}
			return $"{Utilities.EscapeReservedKeyword(field.GeneratedName)} = {varName}";
		}));
		sb.AppendLine($@"
return new {messageName}
{{
    {assignments}
}};
");
	}

	/// <summary>
	/// Appends deserialization logic for a field based on its type to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information to deserialize.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected void AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string varName = GetVariableName(field.GeneratedName);

		if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			AppendArrayFieldDeserialization(sb, arrayType, ref offset, varName);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType)
		{
			AppendEnumFieldDeserialization(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
		{
			AppendArrayEnumFieldDeserialization(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType simpleType)
		{
			AppendSimpleFieldDeserialization(sb, simpleType, ref offset, varName);
		}
	}

	/// <summary>
	/// Appends deserialization logic for optional fields (those not required) to the specified StringBuilder.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="offset">The current byte offset in the payload.</param>
	/// <param name="namespace">The current namespace of the generated message.</param>
	protected void HandleOptionalFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string @namespace)
	{
		foreach (var field in fields.Where(f => !f.IsRequired))
		{
			if (ShouldDeserializeField(field))
			{
				AppendFieldDeserialization(sb, field, ref offset, @namespace);
			}
		}
	}

	/// <summary>
	/// Determines whether the specified field should be deserialized based on its size.
	/// </summary>
	/// <param name="field">The field to evaluate for deserialization.</param>
	/// <returns>
	/// <c>true</c> if the field is not null and its size is greater than zero; otherwise, <c>false</c>.
	/// </returns>
	protected static bool ShouldDeserializeField(GeneratedMavlinkMessageField field)
	{
		return field != null && field.GetFieldSize() > 0;
	}

	/// <summary>
	/// Wraps the generated method body into a temporary class to produce a <see cref="MethodDeclarationSyntax"/> for a static method.
	/// </summary>
	/// <param name="methodName">The name of the generated method.</param>
	/// <param name="returnType">The return type of the generated method.</param>
	/// <param name="methodBody">The generated method body as a string.</param>
	/// <returns>
	/// A <see cref="MethodDeclarationSyntax"/> representing the generated static method.
	/// </returns>
	protected static MethodDeclarationSyntax WrapMethod(string methodName, string returnType, string methodBody)
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

	/// <summary>
	/// Gets the variable name for a generated field based on its original name.
	/// </summary>
	/// <param name="generatedName">The original generated name of the field.</param>
	/// <returns>
	/// A string representing the variable name adjusted for use in the generated code.
	/// </returns>
	protected static string GetVariableName(string generatedName)
	{
		var lower = char.ToLowerInvariant(generatedName[0]) + generatedName.Substring(1);
		if (lower == DeserializeParameterName)
		{
			return "_" + lower;
		}
		return Utilities.EscapeReservedKeyword(lower);
	}

	/// <summary>
	/// Determines the combined type (byte, ushort, uint, or ulong) based on the total number of bits.
	/// </summary>
	/// <param name="totalBits">The total number of bits for the field.</param>
	/// <returns>
	/// A string representing the combined type to use for bit manipulation.
	/// </returns>
	protected static string GetCombinedTypeForTotalBits(int totalBits)
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

	/// <summary>
	/// Gets the fully qualified enum type name based on the provided enum type information and the current namespace.
	/// </summary>
	/// <param name="enumType">The enum field type information.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	/// <returns>
	/// A string representing the fully qualified enum type name.
	/// </returns>
	protected static string GetQualifiedEnumTypeName(GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return enumType.GeneratedEnum.Namespace == currentNamespace
			? enumType.GeneratedEnum.GeneratedName
			: $"{enumType.GeneratedEnum.Namespace}.{enumType.GeneratedEnum.GeneratedName}";
	}
}
