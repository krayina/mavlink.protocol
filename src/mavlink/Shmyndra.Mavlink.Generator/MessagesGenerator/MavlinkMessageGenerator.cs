using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkMessageGenerator
{
	/// <summary>
	/// Generates a C# record declaration for a Mavlink message.
	/// </summary>
	/// <param name="message">The Mavlink message to be processed.</param>
	/// <param name="namespace">The namespace in which the generated message type will reside.</param>
	/// <param name="generatedEnums">A dictionary of generated Mavlink enums used in the message fields or parameters.</param>
	/// <returns>The generated Mavlink message, including its declaration syntax and metadata.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/>, <paramref name="generatedEnums"/>, or <paramref name="message"/> is <c>null</c>.</exception>
	/// <exception cref="InvalidOperationException">Thrown when a message with the same name in the specified namespace has already been generated.</exception>
	/// <remarks>
	/// This method ensures that all enums referenced in the message are resolved using the <paramref name="generatedEnums"/> dictionary.
	/// It also caches the generated messages to avoid duplicate generation. If a message with the same name in the specified namespace has already been generated, an <see cref="InvalidOperationException"/> will be thrown.
	/// </remarks>
	public GeneratedMavlinkMessage GenerateMavlinkMessage(
		MavlinkMessage message,
		string @namespace,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums);
}

public class MavlinkMessageGenerator : IMavlinkMessageGenerator
{
	private static readonly Dictionary<string, (string TypeName, int Size)> _typeMap = new()
	{
		{ "char", ("char", 1) },
		{ "uint8_t", ("byte", 1) },
		{ "int8_t", ("sbyte", 1) },
		{ "uint16_t", ("ushort", 2) },
		{ "int16_t", ("short", 2) },
		{ "uint32_t", ("uint", 4) },
		{ "int32_t", ("int", 4) },
		{ "uint64_t", ("ulong", 8) },
		{ "int64_t", ("long", 8) },
		{ "float", ("float", 4) },
		{ "double", ("double", 8) },
		{ "uint8_t_mavlink_version", ("byte", 1) }
	};

	private readonly Dictionary<(string Namespace, string MavlinkMessageName), GeneratedMavlinkMessage> _generatedMessages = new();

	public GeneratedMavlinkMessage GenerateMavlinkMessage(
		MavlinkMessage message,
		string @namespace,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums)
	{
		if (_generatedMessages.ContainsKey((@namespace, message.Name)))
		{
			throw new InvalidOperationException($"The message '{message.Name}' in namespace '{@namespace}' has already been generated.");
		}

		var generatedMessage = GenerateMavlinkMessageInternal(message, @namespace, generatedEnums);
		_generatedMessages.Add((@namespace, message.Name), generatedMessage);
		return generatedMessage;
	}

	/// <summary>
	/// Generates a <see cref="GeneratedMavlinkMessage"/> instance based on the provided Mavlink message.
	/// </summary>
	/// <param name="message">The Mavlink message to be processed.</param>
	/// <param name="namespace">The namespace in which the generated message type will reside.</param>
	/// <param name="generatedEnums">A dictionary of generated Mavlink enums used in the message fields or parameters.</param>
	/// <returns>The generated Mavlink message, including its declaration syntax and metadata.</returns>
	/// <remarks>
	/// This method creates a new <see cref="GeneratedMavlinkMessage"/> instance but does not add it to the cache.
	/// It is used internally by the <see cref="GenerateMavlinkMessage"/> method to perform the actual message generation logic.
	/// </remarks>
	internal GeneratedMavlinkMessage GenerateMavlinkMessageInternal(
		MavlinkMessage message,
		string @namespace,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums)
	{
		var normalizedName = Utilities.ToCamelCase(message.Name);
		var id = message.Id;

		var generatedFields = message.Fields
			.Select(field => GenerateMavlinkMessageFieldInternal(field, generatedEnums, @namespace, normalizedName))
			.ToImmutableArray();

		var propertyDeclarations = generatedFields
			.Select(generatedField => generatedField.DeclarationSyntax)
			.ToArray();

		var createInstanceMethod = MavlinkMessagePayloadDeserializationGenerator
			.CreateCreateInstanceMethod(@namespace, generatedFields);

		var recordDeclaration = CreateRecordStructDeclaration(id, normalizedName, propertyDeclarations, message.Description, message.Name)
			.AddMembers(createInstanceMethod);

		return new GeneratedMavlinkMessage(@namespace, normalizedName, generatedFields, recordDeclaration, message);
	}

	internal GeneratedMavlinkMessageField GenerateMavlinkMessageFieldInternal(
		MavlinkMessageField field,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums,
		string messageNamespace,
		string messageName)
	{
		const string immutableArrayNamespace = "System.Collections.Immutable.ImmutableArray";
		string normalizedFieldName = Utilities.ToCamelCase(field.Name);
		bool isArray = field.Type.TypeName.Contains("[");

		GeneratedMavlinkMessageFieldType fieldType;
		PropertyDeclarationSyntax propertySyntax;

		if (isArray)
		{
			var baseTypeName = field.Type.TypeName.Split('[')[0];
			var arrayLength = int.Parse(field.Type.TypeName.Split('[', ']')[1]);

			if (field.Type is MavlinkMessageFieldEnumType enumType)
			{
				var generatedEnumForArray = generatedEnums[enumType.EnumName];
				var enumNamespace = generatedEnumForArray.Namespace;
				var enumTypeName = generatedEnumForArray.GeneratedName;

				var fullEnumTypeName = enumNamespace != messageNamespace ? $"{enumNamespace}.{enumTypeName}" : enumTypeName;

				fieldType = new GeneratedMavlinkMessageFieldArrayEnumType(
					field.Type.TypeName,
					_typeMap[baseTypeName].TypeName,
					generatedEnumForArray,
					arrayLength);
				propertySyntax = CreatePropertyDeclaration($"{immutableArrayNamespace}<{fullEnumTypeName}>", normalizedFieldName)
								 .AddArrayLengthAttribute(arrayLength);
			}
			else
			{
				var convertedType = _typeMap[baseTypeName].TypeName;
				fieldType = new GeneratedMavlinkMessageFieldArrayType(
					field.Type.TypeName,
					convertedType,
					arrayLength);
				propertySyntax = CreatePropertyDeclaration($"{immutableArrayNamespace}<{convertedType}>", normalizedFieldName)
								 .AddArrayLengthAttribute(arrayLength);
			}
		}
		else if (field.Type is MavlinkMessageFieldEnumType enumType)
		{
			var generatedEnum = generatedEnums[enumType.EnumName];
			var enumNamespace = generatedEnum.Namespace;
			var enumTypeName = generatedEnum.GeneratedName;

			var fullEnumTypeName = enumNamespace != messageNamespace ? $"{enumNamespace}.{enumTypeName}" : enumTypeName;

			fieldType = new GeneratedMavlinkMessageFieldEnumType(enumType.TypeName, _typeMap[enumType.TypeName].TypeName, generatedEnum);
			propertySyntax = CreatePropertyDeclaration(fullEnumTypeName, normalizedFieldName);
		}
		else
		{
			var convertedType = _typeMap[field.Type.TypeName].TypeName;
			fieldType = new GeneratedMavlinkMessageFieldType(field.Type.TypeName, convertedType);
			propertySyntax = CreatePropertyDeclaration(convertedType, normalizedFieldName);
		}

		propertySyntax = propertySyntax
			.AddSummaryTriviaIfNotNull(field.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {field.Name.ToUpper()}");

		return new GeneratedMavlinkMessageField(
			normalizedFieldName,
			fieldType,
			propertySyntax,
			field);
	}

	private static PropertyDeclarationSyntax CreatePropertyDeclaration(string propertyType, string fieldName)
	{
		return SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propertyType), fieldName)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAccessorListAccessors(
				SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
				SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
	}

	private RecordDeclarationSyntax CreateRecordStructDeclaration(uint id, string normalizedName, PropertyDeclarationSyntax[] properties, string? description, string originalName)
	{
		return SyntaxFactory.RecordDeclaration(
				kind: SyntaxKind.RecordStructDeclaration,
				attributeLists: default,
				modifiers: default,
				keyword: SyntaxFactory.Token(SyntaxKind.RecordKeyword),
				classOrStructKeyword: SyntaxFactory.Token(SyntaxKind.StructKeyword),
				identifier: SyntaxFactory.Identifier(normalizedName),
				typeParameterList: null,
				parameterList: null,
				baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
					SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("MavlinkMessage"))
				)),
				constraintClauses: default,
				openBraceToken: SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
				members: SyntaxFactory.List<MemberDeclarationSyntax>(properties),
				closeBraceToken: SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
				semicolonToken: default)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName(nameof(MavlinkTypes.MavlinkIdentifiedTypeAttribute)[0..^9]))
						.WithArgumentList(
							SyntaxFactory.AttributeArgumentList(
								SyntaxFactory.SeparatedList(new[]
								{
									SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(id))),
									SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(originalName)))
								})
							)
						)
					)
				)
			)
			.AddSummaryTriviaIfNotNull(description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {originalName.ToUpper()}");
	}
}
