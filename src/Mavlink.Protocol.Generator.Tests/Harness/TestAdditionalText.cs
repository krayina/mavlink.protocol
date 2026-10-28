using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Harness;

/// <summary>
/// An in-memory AdditionalFiles item. Paths should be absolute, as the compiler always
/// passes absolute paths to generators.
/// </summary>
internal sealed class TestAdditionalText : AdditionalText
{
	private readonly SourceText _text;

	public TestAdditionalText(string path, string content)
	{
		Path = path;
		_text = SourceText.From(content, Encoding.UTF8);
	}

	public override string Path { get; }

	public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
}
