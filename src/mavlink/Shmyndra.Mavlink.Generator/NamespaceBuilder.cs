using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Builds a C# namespace with Roslyn syntax nodes, supporting member declarations and specific using directives for code generation.
/// </summary>
public class NamespaceBuilder
{
	private readonly string _namespaceName;
	private readonly List<MemberDeclarationSyntax> _members = new();
	private readonly List<string> _specificUsings = new();

	public NamespaceBuilder(string namespaceName)
	{
		_namespaceName = namespaceName;
	}

	/// <summary>
	/// Adds a member (e.g., struct, enum) to the namespace.
	/// </summary>
	/// <param name="member">The member to add.</param>
	/// <returns>This instance for chaining.</returns>
	public NamespaceBuilder AddMember(MemberDeclarationSyntax member)
	{
		_members.Add(member);
		return this;
	}

	/// <summary>
	/// Adds a using directive specific to this namespace.
	/// </summary>
	/// <param name="usingDirective">The using directive (e.g., "System").</param>
	/// <returns>This instance for chaining.</returns>
	public NamespaceBuilder AddSpecificUsing(string usingDirective)
	{
		_specificUsings.Add(usingDirective);
		return this;
	}

	/// <summary>
	/// Builds the namespace as a Roslyn syntax node.
	/// </summary>
	/// <returns>The constructed namespace.</returns>
	public NamespaceDeclarationSyntax Build()
	{
		return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(_namespaceName))
			.WithMembers(SyntaxFactory.List(_members))
			.NormalizeWhitespace();
	}
}
