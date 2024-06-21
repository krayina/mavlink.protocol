using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal static class MessageProcessor
{
	public static IEnumerable<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> ParseMessages(IEnumerable<string> xmlContents, ImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var serializer = new XmlSerializer(typeof(Mavlink));
		var messageDict = new Dictionary<string, (string? Description, List<(string Type, string Name, string? Description)> Fields)>();

		foreach (var xmlContent in xmlContents)
		{
			using var reader = new StringReader(xmlContent);
			var mavlink = (Mavlink)serializer.Deserialize(reader);
			foreach (var m in mavlink.Messages)
			{
				var name = m.Name;
				var fields = m.Field.Select(field =>
				{
					var fieldType = ConvertType(field.Type);
					if (field.Enum is not null && enumTypes.ContainsKey(field.Enum))
					{
						fieldType = enumTypes[field.Enum].TypeName;
					}
					return (fieldType, field.Name, field.Description);
				}).ToList();

				if (messageDict.ContainsKey(name))
				{
					messageDict[name].Fields.AddRange(fields);
				}
				else
				{
					messageDict[name] = (m.Description, fields);
				}
			}
		}

		return messageDict.Select(kv => (kv.Key, kv.Value.Description, kv.Value.Fields));
	}

	public static RecordDeclarationSyntax CreateRecordStruct((string Name, string? Description, List<(string Type, string Name, string? Description)> Fields) messageData, string namespaceName, ImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
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

	private static string GetTypeName(string xmlType, string currentNamespace, ImmutableDictionary<string, (string Namespace, string TypeName)> generatedTypes)
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

	private static string ConvertType(string xmlType)
	{
		return xmlType switch
		{
			"uint8_t" => "byte",
			"int8_t" => "sbyte",
			"uint16_t" => "ushort",
			"int16_t" => "short",
			"uint32_t" => "uint",
			"int32_t" => "int",
			"uint64_t" => "ulong",
			"int64_t" => "long",
			"float" => "float",
			"double" => "double",
			_ => "object"
		};
	}
}
