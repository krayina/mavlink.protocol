using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mavlink.Protocol.Generator.Linking;

/// <summary>
/// The project's own dialect files: additional files marked with <c>MavlinkDialect="true"</c>
/// metadata. Other additional files, XML or not, are not the generator's business.
/// </summary>
internal static class DialectFiles
{
	/// <summary>
	/// Item metadata that marks an own dialect file; the build props set it on <c>MavlinkDialect</c> items.
	/// </summary>
	public const string MetadataName = "MavlinkDialect";

	private const string MetadataKey = "build_metadata.AdditionalFiles." + MetadataName;

	public static bool IsOwned(AdditionalText file, AnalyzerConfigOptionsProvider options)
		=> options.GetOptions(file).TryGetValue(MetadataKey, out var value)
			&& string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase);

	public static ParsedFile Read(AdditionalText file, CancellationToken cancellationToken)
	{
		var text = file.GetText(cancellationToken);
		if (text is not null)
		{
			return MavlinkXmlReader.Read(file.Path, text, cancellationToken);
		}

		var diagnostic = DiagnosticInfo.Create(
			DiagnosticDescriptors.InvalidXml,
			new LocationInfo(file.Path, default, default),
			"The file could not be read.");
		return new ParsedFile(
			file.Path,
			string.Empty,
			EquatableArray<ParsedInclude>.Empty,
			null,
			null,
			EquatableArray<ParsedEnum>.Empty,
			EquatableArray<ParsedMessage>.Empty,
			new[] { diagnostic }.ToEquatableArray());
	}
}
