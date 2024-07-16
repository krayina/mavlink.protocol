using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkMessageFactory
{
	public static MethodDeclarationSyntax GenerateCreateInstanceMethod(
		string messageTypeName,
		ImmutableList<(FieldType Type, string Name, string? Description)> fields,
		IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var statements = new List<StatementSyntax>();

		foreach (var field in fields)
		{
			var fieldType = field.Type;
			var fieldName = field.Name;
			var variableName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);

			StatementSyntax deserializationStatement;
			if (fieldType is FieldArrayType arrayType)
			{
				var elementType = ExtractElementType(arrayType.TypeName);
				deserializationStatement = GenerateArrayDeserialization(variableName, arrayType, elementType);
			}
			else if (enumTypes.ContainsKey(fieldType.TypeName))
			{
				deserializationStatement = GenerateEnumDeserialization(variableName, fieldType.TypeName, enumTypes);
			}
			else
			{
				deserializationStatement = GenerateSimpleTypeDeserialization(variableName, fieldType.TypeName);
			}

			statements.Add(deserializationStatement);
		}

		var propertiesAssignment = fields.Select(field =>
		{
			var variableName = char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
			return SyntaxFactory.AssignmentExpression(
				SyntaxKind.SimpleAssignmentExpression,
				SyntaxFactory.IdentifierName(field.Name),
				SyntaxFactory.IdentifierName(variableName)) as ExpressionSyntax;
		}).ToArray();

		var returnStatement = SyntaxFactory.ReturnStatement(
			SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(messageTypeName))
			.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, SyntaxFactory.SeparatedList(propertiesAssignment))));

		statements.Add(returnStatement);

		var body = SyntaxFactory.Block(statements);
		var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(messageTypeName), "CreateInstance")
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
			.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("payload"))
				.WithType(SyntaxFactory.ParseTypeName("byte[]")))
			.WithBody(body);

		return method;
	}

	private static string ExtractElementType(string typeName)
	{
		var start = typeName.IndexOf('<') + 1;
		var length = typeName.IndexOf('>') - start;
		return typeName.Substring(start, length);
	}

	private static StatementSyntax GenerateArrayDeserialization(string variableName, FieldArrayType arrayType, string elementType)
	{
		return SyntaxFactory.ParseStatement($@"
            var temp{variableName}Array = new {elementType}[{arrayType.Length}];
            Buffer.BlockCopy(payload, 0, temp{variableName}Array, 0, {arrayType.Length} * sizeof({elementType}));
            var {variableName} = temp{variableName}Array.ToImmutableArray();
        ");
	}

	private static StatementSyntax GenerateEnumDeserialization(string variableName, string typeName, IImmutableDictionary<string, (string Namespace, string TypeName)> enumTypes)
	{
		var enumType = enumTypes[typeName];
		return SyntaxFactory.ParseStatement($@"
            var {variableName}Value = BitConverter.ToInt32(payload, 0); // Assuming 4-byte enum
            var {variableName} = ({enumType.Namespace}.{enumType.TypeName}){variableName}Value;
        ");
	}

	private static StatementSyntax GenerateSimpleTypeDeserialization(string variableName, string typeName)
	{
		var deserializationCode = typeName switch
		{
			"sbyte" => $"var {variableName} = (sbyte)payload[0];",
			"byte" => $"var {variableName} = payload[0];",
			_ => $"var {variableName} = BitConverter.{GetBitConverterMethod(typeName)}(payload, 0);"
		};
		return SyntaxFactory.ParseStatement(deserializationCode);
	}

	private static string GetBitConverterMethod(string typeName)
	{
		return typeName switch
		{
			"int" => "ToInt32",
			"uint" => "ToUInt32",
			"short" => "ToInt16",
			"ushort" => "ToUInt16",
			"long" => "ToInt64",
			"ulong" => "ToUInt64",
			"float" => "ToSingle",
			"double" => "ToDouble",
			_ => throw new NotSupportedException($"Unsupported type: {typeName}")
		};
	}
}
