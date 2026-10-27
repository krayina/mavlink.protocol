using System.ComponentModel;

namespace Mavlink.Dialects.Infrastructure;

/// <summary>
/// Emitted by the generator once per dialect XML file an assembly owns. The generator in a
/// referencing project reads it to learn which XML files are already generated, into which
/// namespace and with which includes, without needing the XML itself.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class MavlinkDialectManifestAttribute : Attribute
{
	/// <summary>
	/// The manifest format emitted and understood by the current generator.
	/// </summary>
	public const int CurrentFormatVersion = 1;

	public MavlinkDialectManifestAttribute(int formatVersion, string name, string @namespace, Type dialect)
	{
		FormatVersion = formatVersion;
		Name = name;
		Namespace = @namespace;
		Dialect = dialect;
	}

	/// <summary>
	/// Format of this manifest; a generator rejects formats it does not understand.
	/// </summary>
	public int FormatVersion { get; }

	/// <summary>
	/// Stem of the dialect XML file, e.g. "minimal".
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Namespace of the types declared by the XML file, e.g. "Mavlink.Minimal".
	/// </summary>
	public string Namespace { get; }

	/// <summary>
	/// The generated <see cref="IMavlinkDialect"/> implementation of the XML file.
	/// </summary>
	public Type Dialect { get; }

	/// <summary>
	/// Stems of the XML files this one includes directly.
	/// </summary>
	public string[] Includes { get; set; } = Array.Empty<string>();

	/// <summary>
	/// Lowercase hex SHA-256 of the XML text (UTF-8, BOM stripped, CRLF normalized to LF).
	/// </summary>
	public string? XmlHash { get; set; }
}
