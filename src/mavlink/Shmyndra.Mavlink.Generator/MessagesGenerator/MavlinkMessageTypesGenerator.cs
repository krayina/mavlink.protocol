using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkMessageTypesGenerator
{
	/// <summary>
	/// Generates C# record declarations for Mavlink messages.
	/// </summary>
	/// <param name="messages">A collection of Mavlink messages to be processed.</param>
	/// <param name="namespaceName">The namespace in which the generated message types will reside.</param>
	/// <param name="generatedEnums">A dictionary of generated Mavlink enums used in the message fields or parameters.</param>
	/// <param name="generatedTypes">An output dictionary that maps the message names to the corresponding generated message types.</param>
	/// <returns>A list of C# record declarations representing the Mavlink messages.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespaceName"/>, <paramref name="generatedEnums"/>, or <paramref name="messages"/> is <c>null</c>.</exception>
	/// <remarks>
	/// The method ensures that all enums referenced in the messages are resolved using the <paramref name="generatedEnums"/> dictionary.
	/// The output <paramref name="generatedTypes"/> dictionary will contain the fully qualified names of the generated message types for reference in other parts of the codebase.
	/// </remarks>
	List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<MavlinkMessage> messages,
		string namespaceName,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums,
		out IImmutableDictionary<string, GeneratedMavlinkMessage> generatedTypes);
}

public class MavlinkMessageTypesGenerator : IMavlinkMessageTypesGenerator
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

	private readonly HashSet<string> _generatedMessageNames = new();

	public List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<MavlinkMessage> messages,
		string namespaceName,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums,
		out IImmutableDictionary<string, GeneratedMavlinkMessage> generatedTypes)
	{
		var generatedTypesDict = new Dictionary<string, GeneratedMavlinkMessage>();
		var messageDeclarations = new List<RecordDeclarationSyntax>();

		foreach (var messageData in messages)
		{
			if (_generatedMessageNames.Contains(messageData.Name))
			{
				continue;
			}

			var generatedMessage = CreateMavlinkMessage(messageData, namespaceName, generatedEnums);
			messageDeclarations.Add(generatedMessage.DeclarationSyntax);
			generatedTypesDict[messageData.Name] = generatedMessage;
			_generatedMessageNames.Add(messageData.Name);
		}

		generatedTypes = generatedTypesDict.ToImmutableDictionary();
		return messageDeclarations;
	}

	private GeneratedMavlinkMessage CreateMavlinkMessage(
		MavlinkMessage messageData,
		string namespaceName,
		IImmutableDictionary<string, GeneratedMavlinkEnum> generatedEnums)
	{
		var normalizedName = Utilities.ToCamelCase(messageData.Name);
		var id = messageData.Id;

		var generatedFields = messageData.Fields
			.Select(field => CreateField(field, generatedEnums, namespaceName, normalizedName))
			.ToImmutableArray();

		var propertyDeclarations = generatedFields
			.Select(generatedField => generatedField.DeclarationSyntax)
			.ToArray();

		var createInstanceMethod = MavlinkMessagePayloadDeserializationGenerator
			.GenerateCreateInstanceMethod(namespaceName, generatedFields);

		var recordDeclaration = CreateRecordStructDeclaration(id, normalizedName, propertyDeclarations, messageData.Description, messageData.Name)
			.AddMembers(createInstanceMethod);

		return new GeneratedMavlinkMessage(namespaceName, normalizedName, generatedFields, recordDeclaration, messageData);
	}

	private GeneratedMavlinkMessageField CreateField(
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
