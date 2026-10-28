namespace Mavlink.Protocol.Generator.Linking;

/// <summary>
/// A dialect is identified by its XML file name without the extension (<c>common</c>),
/// the same key the manifest and the namespace use. Folders in a path or an include do not
/// matter, and neither does letter case, so the result is the same on every OS.
/// </summary>
internal static class DialectName
{
	public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

	private static readonly char[] Separators = { '/', '\\' };

	/// <summary>
	/// <c>common</c> for <c>common.xml</c>, <c>../xml/common.xml</c> or <c>C:\xml\Common.XML</c>.
	/// </summary>
	public static string FromPath(string path)
	{
		var start = path.LastIndexOfAny(Separators) + 1;
		var name = path.Substring(start);
		return name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
			? name.Substring(0, name.Length - 4)
			: name;
	}
}
