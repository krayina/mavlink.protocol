using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Diagnostics;

/// <summary>
/// An equatable stand-in for <see cref="Location"/>, which pipeline models cannot hold
/// without breaking caching. Points into a source or an additional file (XML).
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
	public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);

	/// <summary>
	/// A location of <paramref name="length"/> characters starting at a 1-based line and column,
	/// as <see cref="System.Xml.IXmlLineInfo"/> reports them.
	/// </summary>
	public static LocationInfo FromLineColumn(string filePath, SourceText text, int line, int column, int length)
	{
		if (line < 1 || line > text.Lines.Count)
		{
			return new LocationInfo(filePath, default, default);
		}

		var textLine = text.Lines[line - 1];
		var start = Math.Min(textLine.Start + Math.Max(column - 1, 0), textLine.End);
		var end = Math.Min(start + Math.Max(length, 0), textLine.End);
		var span = TextSpan.FromBounds(start, end);
		return new LocationInfo(filePath, span, text.Lines.GetLinePositionSpan(span));
	}
}
