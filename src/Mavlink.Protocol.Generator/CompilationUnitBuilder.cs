using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Builds a C# compilation unit with a namespace, supporting member declarations and specific using directives for code generation.
/// </summary>
public class CompilationUnitBuilder
{
	private readonly string _namespaceName;
	private readonly List<MemberDeclarationSyntax> _members = new();
	private readonly List<string> _specificUsings = new();

	public CompilationUnitBuilder(string namespaceName)
	{
		_namespaceName = namespaceName;
	}

	/// <summary>
	/// Adds a member (e.g., struct, enum) to the namespace.
	/// </summary>
	/// <param name="member">The member to add.</param>
	/// <returns>This instance for chaining.</returns>
	public CompilationUnitBuilder AddMember(MemberDeclarationSyntax member)
	{
		_members.Add(member);
		return this;
	}

	/// <summary>
	/// Adds a member (e.g., struct, enum) to the namespace from a string representation.
	/// </summary> 
	/// <param name="memberCode">The string containing the member code.</param>
	/// <returns>This instance for chaining.</returns>
	/// <exception cref="ArgumentException">Thrown when the member code cannot be parsed.</exception>
	public CompilationUnitBuilder AddMember(string memberCode)
	{
		var member = SyntaxFactory.ParseMemberDeclaration(memberCode);
		if (member == null)
		{
			throw new ArgumentException("Invalid member code provided.", nameof(memberCode));
		}
		_members.Add(member);
		return this;
	}

	/// <summary>
	/// Adds a using directive specific to this namespace.
	/// </summary>
	/// <param name="usingDirective">The using directive (e.g., "System").</param>
	/// <returns>This instance for chaining.</returns>
	public CompilationUnitBuilder AddSpecificUsing(string usingDirective)
	{
		_specificUsings.Add(usingDirective);
		return this;
	}

	/// <summary>
	/// Builds the compilation unit containing the namespace and using directives as a Roslyn syntax node.
	/// </summary>
	/// <returns>The constructed compilation unit.</returns>
	public CompilationUnitSyntax Build()
	{
		var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(_namespaceName))
			.WithMembers(SyntaxFactory.List(_members));

		var usingDirectives = _specificUsings
			.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)))
			.ToList();

		return SyntaxFactory.CompilationUnit()
			.WithUsings(SyntaxFactory.List(usingDirectives))
			.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(namespaceDeclaration))
			.NormalizeWhitespace();
	}
}
