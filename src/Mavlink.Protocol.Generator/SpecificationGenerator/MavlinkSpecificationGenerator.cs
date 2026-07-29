using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Mavlink.Protocol.Generator;

public interface IMavlinkSpecificationGenerator
{
	ClassDeclarationSyntax GenerateSpecification(MavlinkData mavlinkData);
}

public class MavlinkSpecificationGenerator : IMavlinkSpecificationGenerator
{
	public ClassDeclarationSyntax GenerateSpecification(MavlinkData mavlinkData)
	{
		var versionProperty = CreateProperty("Version", "byte?", mavlinkData.Version);
		var dialectProperty = CreateProperty("Dialect", "byte?", mavlinkData.Dialect);

		var classDeclaration = SyntaxFactory.ClassDeclaration("MavlinkSpecification")
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
			.AddMembers(versionProperty, dialectProperty);

		return classDeclaration;
	}

	private PropertyDeclarationSyntax CreateProperty(string name, string type, byte? value)
	{
		return SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(type), name)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
			.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
				value.HasValue ? SyntaxKind.NumericLiteralExpression : SyntaxKind.NullLiteralExpression,
				value.HasValue ? SyntaxFactory.Literal(value.Value) : SyntaxFactory.Token(SyntaxKind.NullKeyword))))
			.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
	}
}
