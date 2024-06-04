using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Xml.Serialization;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal static class MessageProcessor
{
	public static IEnumerable<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> ParseMessages(IEnumerable<string> xmlContents)
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
				var fields = m.Field.Select(field => (ConvertType(field.Type), field.Name, field.Description)).ToList();

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

	public static void GenerateMessageFile(SourceProductionContext context, ImmutableArray<(string Name, string? Description, List<(string Type, string Name, string? Description)> Fields)> messages)
	{
		if (messages.IsDefaultOrEmpty)
			return;

		var compilationUnit = SyntaxFactory.CompilationUnit()
			.AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedMavlink"))
				.AddMembers(messages.Select(CreateRecordStruct).ToArray()));

		context.AddSource("MavlinkMessages.g.cs", SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
	}

	private static RecordDeclarationSyntax CreateRecordStruct((string Name, string? Description, List<(string Type, string Name, string? Description)> Fields) messageData)
	{
		var normalizedName = Utilities.ToCamelCase(messageData.Name);

		var properties = messageData.Fields.Select(field =>
		{
			var normalizedFiledName = Utilities.ToCamelCase(field.Name);
			var fieldName = normalizedFiledName == normalizedName ? "_" + normalizedFiledName : normalizedFiledName;
			var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(field.Type), fieldName)
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
						SyntaxFactory.Attribute(
							SyntaxFactory.ParseName("Shmyndra.Mavlink.SourceGenerators.Protocol.MavlinkType")))))
			.AddSummaryTriviaIfNotNull(messageData.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {messageData.Name.ToUpper()}");

		return recordStruct;
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
