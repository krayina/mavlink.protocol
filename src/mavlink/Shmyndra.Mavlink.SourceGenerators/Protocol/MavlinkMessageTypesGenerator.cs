using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

public interface IMavlinkMessageTypesGenerator
{
	List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		ImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes,
		out ImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping);
}

public class MavlinkMessageTypesGenerator : IMavlinkMessageTypesGenerator
{
	private readonly HashSet<string> _generatedMessageNames = new();

	public List<RecordDeclarationSyntax> GenerateMessages(
		ImmutableArray<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		ImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes,
		out ImmutableDictionary<string, (string Namespace, string TypeName)> nameMapping)
	{
		var nameMappingDict = new Dictionary<string, (string Namespace, string TypeName)>();
		var messageDeclarations = new List<RecordDeclarationSyntax>();

		foreach (var messageData in messages)
		{
			if (_generatedMessageNames.Contains(messageData.Name))
			{
				continue;
			}

			var recordStruct = GenerateRecordStruct(messageData, namespaceName, enumTypes);
			messageDeclarations.Add(recordStruct);
			nameMappingDict[messageData.Name] = (namespaceName, recordStruct.Identifier.Text);
			_generatedMessageNames.Add(messageData.Name);
		}

		nameMapping = nameMappingDict.ToImmutableDictionary();
		return messageDeclarations;
	}

	private RecordDeclarationSyntax GenerateRecordStruct(
		(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields) messageData,
		string namespaceName,
		ImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
	{
		var normalizedName = Utilities.ToCamelCase(messageData.Name);

		var properties = messageData.Fields.Select(field =>
		{
			var normalizedFieldName = Utilities.ToCamelCase(field.Name);
			var fieldName = normalizedFieldName == normalizedName ? "_" + normalizedFieldName : normalizedFieldName;
			var propertyType = GetTypeName(field.Type, namespaceName, generatedTypes);
			var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propertyType), fieldName)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.AddAccessorListAccessors(
					SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
					SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
				.AddSummaryTriviaIfNotNull(field.Description)
				.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {field.Name.ToUpper()}");
			return property;
		}).ToArray();

		var recordStruct = SyntaxFactory
			.RecordDeclaration(
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
						SyntaxFactory.Attribute(SyntaxFactory.ParseName("Shmyndra.Mavlink.SourceGenerators.Protocol.MavlinkType"))
						.WithArgumentList(
							SyntaxFactory.AttributeArgumentList(
								SyntaxFactory.SeparatedList(new[]
								{
									SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
										SyntaxKind.StringLiteralExpression,
										SyntaxFactory.Literal(messageData.Name))
									)
								})
							)
						)
					)
				)
			)
			.AddSummaryTriviaIfNotNull(messageData.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {messageData.Name.ToUpper()}");

		return recordStruct;
	}

	private string GetTypeName(string xmlType, string currentNamespace, ImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
	{
		// Extract the type name if it includes the namespace
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

		// If type is not a generated type, return it as is (assuming it's a standard .NET type)
		return xmlType;
	}
}
