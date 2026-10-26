namespace Mavlink.Dialects;

public interface IMavlinkDialect
{
	/// <summary>
	/// Dialect name: the stem of the XML file it was generated from (e.g. "common").
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Dialects this one includes directly (its &lt;include&gt; elements). Messages of
	/// included dialects are resolved by <see cref="MavlinkDialects.Combine"/>, not by this dialect.
	/// </summary>
	IReadOnlyList<IMavlinkDialect> Includes { get; }

	IMavlinkMessageInfo? GetInfo(uint msgId);
	IMavlinkMessageInfo? GetInfo(Type type);
}
