using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shmyndra.Mavlink.Generator.Data;
using System.Collections.Immutable;
using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// An abstract base class responsible for generating deserialization methods for Mavlink messages,
/// converting byte streams into instances of the generated message types. This class provides a unified
/// framework for deserializing fields with and without validation rules (e.g., 'invalid' attribute),
/// supporting different field types such as arrays, enums, and simple types.
/// </summary>
/// <remarks>
/// Derived classes must implement specific deserialization logic for fields with and without validation,
/// allowing for flexibility in handling buffer-based or span-based deserialization strategies.
/// The class separates the deserialization of regular fields (without validation) from fields with 
/// validation rules, ensuring efficient processing and extensibility.
/// </remarks>
public abstract class MavlinkMessageDeserializationMethodGeneratorBase
{
	protected const string CreateRangeWithNamespace = "System.Collections.Immutable.ImmutableArray.CreateRange";
	protected const string DeserializeParameterName = "payload";
	protected const string DeserializeWithExtensionsMethodName = "DeserializeWithExtensions";
	protected const string DeserializeWithoutExtensionsMethodName = "DeserializeWithoutExtensions";

	/// <summary>
	/// Creates a deserialization method for a Mavlink message, including both methods with and without optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageDeserializeMethod"/> containing the deserialization methods.</returns>
	public GeneratedMavlinkMessageDeserializeMethod CreateDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
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
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	internal MethodDeclarationSyntax CreateDeserializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int minSize = fields.CalculateMinSize();
		int offset = 0;
		var (requiredFields, arrayFields) = fields.GetSortedFields();

		AppendMethodPrologue(methodBody, messageName, minSize);
		AppendNonExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendAssignments(methodBody, messageName, fields, @namespace);
		return WrapMethod(DeserializeWithoutExtensionsMethodName, messageName, methodBody.ToString());
	}

	/// <summary>
	/// Generates the DeserializeWithExtensions method for messages with optional extension fields.
	/// </summary>
	/// <param name="namespace">The namespace of the generated message type.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated method.</returns>
	internal MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var methodBody = new StringBuilder();
		int finalSize = fields.CalculateFinalSize();
		int offset = 0;
		var (requiredFields, arrayFields) = fields.GetSortedFields();

		AppendMethodPrologue(methodBody, messageName, finalSize);
		AppendNonExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendExtensionFields(methodBody, fields, ref offset, @namespace);
		AppendAssignments(methodBody, messageName, fields, @namespace);
		return WrapMethod(DeserializeWithExtensionsMethodName, messageName, methodBody.ToString());
	}

	/// <summary>
	/// Appends the prologue for deserialization, including payload length checks and padding logic if necessary.
	/// Derived classes must implement this method to provide type-specific prologue logic.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the prologue code to.</param>
	/// <param name="messageName">The name of the generated message type.</param>
	/// <param name="requiredSize">The minimum required size of the payload.</param>
	protected abstract void AppendMethodPrologue(StringBuilder sb, string messageName, int requiredSize);

	/// <summary>
	/// Appends deserialization logic for a single field, delegating to either default or validation-based logic
	/// depending on the presence of an 'invalid' attribute.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information to deserialize.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected void AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace)
	{
		string varName = GetVariableName(field.GeneratedName);

		if (string.IsNullOrWhiteSpace(field.Invalid))
		{
			AppendFieldDeserializationDefault(sb, field, ref offset, varName, currentNamespace);
		}
		else
		{
			var handler = InvalidFieldHandlerFactory.Create(field);
			if (handler == null) throw new InvalidOperationException($"Немає обробника для поля {field.GeneratedName} з invalid={field.Invalid}");
			AppendFieldDeserializationWithValidation(sb, field, handler, ref offset, varName, currentNamespace);
		}
	}

	/// <summary>
	/// Appends default deserialization logic for a field without validation rules.
	/// This method delegates to type-specific default deserialization methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information to deserialize.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected virtual void AppendFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace)
	{
		if (field.Type is GeneratedMavlinkMessageFieldArrayType)
		{
			AppendArrayFieldDeserializationDefault(sb, field, ref offset, varName);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType)
		{
			AppendEnumFieldDeserializationDefault(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
		{
			AppendArrayEnumFieldDeserializationDefault(sb, field, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType)
		{
			AppendSimpleFieldDeserializationDefault(sb, field, ref offset, varName);
		}
	}

	/// <summary>
	/// Appends deserialization logic for a field with validation rules specified by an 'invalid' attribute.
	/// This method delegates to type-specific validation-based deserialization methods.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information to deserialize.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected virtual void AppendFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName, string currentNamespace)
	{
		if (field.Type is GeneratedMavlinkMessageFieldArrayType)
		{
			AppendArrayFieldDeserializationWithValidation(sb, field, handler, ref offset, varName);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldEnumType)
		{
			AppendEnumFieldDeserializationWithValidation(sb, field, handler, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType)
		{
			AppendArrayEnumFieldDeserializationWithValidation(sb, field, handler, ref offset, varName, currentNamespace);
		}
		else if (field.Type is GeneratedMavlinkMessageFieldType)
		{
			AppendSimpleFieldDeserializationWithValidation(sb, field, handler, ref offset, varName);
		}
	}

	/// <summary>
	/// Appends default deserialization logic for a simple (primitive) field without validation.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing simple type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendSimpleFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName);

	/// <summary>
	/// Appends default deserialization logic for an enum field without validation.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendEnumFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends default deserialization logic for an array field without validation.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendArrayFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName);

	/// <summary>
	/// Appends default deserialization logic for an array of enums without validation.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendArrayEnumFieldDeserializationDefault(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends deserialization logic for a simple (primitive) field with validation rules.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing simple type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendSimpleFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName);

	/// <summary>
	/// Appends deserialization logic for an enum field with validation rules.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing enum type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendEnumFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends deserialization logic for an array field with validation rules.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	protected abstract void AppendArrayFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName);

	/// <summary>
	/// Appends deserialization logic for an array of enums with validation rules.
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="field">The field information containing array enum type details.</param>
	/// <param name="handler">The validation handler providing the condition for invalid values.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="varName">The variable name used for the field in the generated code.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected abstract void AppendArrayEnumFieldDeserializationWithValidation(StringBuilder sb, GeneratedMavlinkMessageField field, IInvalidFieldHandler handler, ref int offset, string varName, string currentNamespace);

	/// <summary>
	/// Appends deserialization logic for non-extension fields (both required and array fields).
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	protected void AppendNonExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string currentNamespace)
	{
		var (requiredFields, arrayFields) = fields.GetSortedFields();
		foreach (var field in requiredFields)
		{
			AppendFieldDeserialization(sb, field, ref offset, currentNamespace);
		}
		foreach (var field in arrayFields)
		{
			AppendFieldDeserialization(sb, field, ref offset, currentNamespace);
		}
	}

	/// <summary>
	/// Appends deserialization logic for optional extension fields (those not required).
	/// </summary>
	/// <param name="sb">The StringBuilder to append the code to.</param>
	/// <param name="fields">An immutable array of fields representing the Mavlink message.</param>
	/// <param name="offset">The current byte offset in the payload, updated during deserialization.</param>
	/// <param name="namespace">The current namespace of the generated message.</param>
	protected void AppendExtensionFields(StringBuilder sb, ImmutableArray<GeneratedMavlinkMessageField> fields, ref int offset, string @namespace)
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
	/// Determines whether the specified field should be deserialized based on its size.
	/// </summary>
	/// <param name="field">The field to evaluate for deserialization.</param>
	/// <returns><c>true</c> if the field is not null and its size is greater than zero; otherwise, <c>false</c>.</returns>
	protected static bool ShouldDeserializeField(GeneratedMavlinkMessageField field)
	{
		return field != null && field.GetFieldSize() > 0;
	}

	/// <summary>
	/// Wraps the generated method body into a temporary class to produce a static method declaration.
	/// </summary>
	/// <param name="methodName">The name of the generated method.</param>
	/// <param name="returnType">The return type of the generated method.</param>
	/// <param name="methodBody">The generated method body as a string.</param>
	/// <returns>A <see cref="MethodDeclarationSyntax"/> representing the generated static method.</returns>
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
	/// <returns>A string representing the variable name adjusted for use in the generated code.</returns>
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
	/// Gets the fully qualified enum type name based on the provided enum type information and the current namespace.
	/// </summary>
	/// <param name="enumType">The enum field type information.</param>
	/// <param name="currentNamespace">The current namespace of the generated message.</param>
	/// <returns>A string representing the fully qualified enum type name.</returns>
	protected static string GetQualifiedEnumTypeName(GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return enumType.GeneratedEnum.Namespace == currentNamespace
			? enumType.GeneratedEnum.GeneratedName
			: $"{enumType.GeneratedEnum.Namespace}.{enumType.GeneratedEnum.GeneratedName}";
	}
}
