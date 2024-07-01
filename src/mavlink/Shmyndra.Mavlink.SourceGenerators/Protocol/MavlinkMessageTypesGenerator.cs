using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public interface IMavlinkMessageTypesGenerator
{
	List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping);
}

public class MavlinkMessageTypesGenerator : IMavlinkMessageTypesGenerator
{
	private readonly HashSet<string> _generatedMessageNames = new();

	public List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes,
		out IImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping)
	{
		var nameMappingDict = new Dictionary<string, (string Namespace, string TypeName)>();
		var messageDeclarations = new List<RecordDeclarationSyntax>();

		foreach (var messageData in messages)
		{
			if (_generatedMessageNames.Contains(messageData.Name))
			{
				continue;
			}

			var recordStruct = CreateRecordStruct(messageData, namespaceName, enumTypes);
			messageDeclarations.Add(recordStruct);
			nameMappingDict[messageData.Name] = (namespaceName, recordStruct.Identifier.Text);
			_generatedMessageNames.Add(messageData.Name);
		}

		nameMapping = nameMappingDict.ToImmutableSortedDictionary();
		return messageDeclarations;
	}

	private RecordDeclarationSyntax CreateRecordStruct(
		(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields) messageData,
		string namespaceName,
		IImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
	{
		var normalizedName = Utilities.ToCamelCase(messageData.Name);

		var properties = messageData.Fields.Select(field =>
		{
			var normalizedFieldName = Utilities.ToCamelCase(field.Name);
			var fieldName = normalizedFieldName == normalizedName ? "_" + normalizedFieldName : normalizedFieldName;

			string propertyType = field.Type switch
			{
				FieldArrayType arrayFieldType => arrayFieldType.TypeName,
				_ => GetTypeName(field.Type.TypeName, namespaceName, generatedTypes)
			};

			var property = CreatePropertyDeclaration(propertyType, fieldName);

			if (field.Type is FieldArrayType arrayType)
			{
				property = AddArrayLengthAttribute(property, arrayType.Length);
			}

			return property.AddSummaryTriviaIfNotNull(field.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {field.Name.ToUpper()}");
		}).ToArray();

		return CreateRecordStructDeclaration(normalizedName, properties, messageData.Description, messageData.Name);
	}

	private PropertyDeclarationSyntax CreatePropertyDeclaration(string propertyType, string fieldName)
	{
		var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propertyType), fieldName)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAccessorListAccessors(
				SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
				SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
		return property;
	}

	private PropertyDeclarationSyntax AddArrayLengthAttribute(PropertyDeclarationSyntax property, int length)
	{
		return property.AddAttributeLists(
			SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(RequiredArrayLengthAttribute).FullName[0..^9]))
					.WithArgumentList(
						SyntaxFactory.AttributeArgumentList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(length))))
						)
					)
				)
			)
		);
	}

	private RecordDeclarationSyntax CreateRecordStructDeclaration(string normalizedName, PropertyDeclarationSyntax[] properties, string? description, string originalName)
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
				baseList: null,
				constraintClauses: default,
				openBraceToken: SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
				members: SyntaxFactory.List<MemberDeclarationSyntax>(properties),
				closeBraceToken: SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
				semicolonToken: default)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAttributeLists(
				SyntaxFactory.AttributeList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(MavlinkTypes.MavlinkTypeAttribute).Name[0..^9]))
						.WithArgumentList(
							SyntaxFactory.AttributeArgumentList(
								SyntaxFactory.SeparatedList(new[]
								{
								SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
									SyntaxKind.StringLiteralExpression,
									SyntaxFactory.Literal(originalName))
								)
								})
							)
						)
					)
				)
			)
			.AddSummaryTriviaIfNotNull(description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {originalName.ToUpper()}");
	}

	private string GetTypeName(string xmlType, string currentNamespace, IImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
	{
		var typeParts = xmlType.Split('.');
		var typeName = typeParts.Length > 1 ? typeParts.Last() : xmlType;

		if (generatedTypes.TryGetValue(typeName, out var generatedTypeInfo))
		{
			if (generatedTypeInfo.Namespace != currentNamespace)
			{
				return $"{generatedTypeInfo.Namespace}.{generatedTypeInfo.TypeName}";
			}
			else
			{
				return generatedTypeInfo.TypeName;
			}
		}

		return xmlType;
	}
}
