using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents Mavlink data with enums, messages, includes, version, and dialect.
/// </summary>
public record MavlinkData
{
	/// <summary>
	/// The collection of Mavlink enums.
	/// </summary>
	public ImmutableArray<MavlinkEnum> Enums { get; init; }

	/// <summary>
	/// The collection of Mavlink messages.
	/// </summary>
	public ImmutableArray<MavlinkMessage> Messages { get; init; }

	/// <summary>
	/// The collection of include directives.
	/// </summary>
	public ImmutableArray<string> Includes { get; init; }

	/// <summary>
	/// The version of the Mavlink protocol.
	/// </summary>
	public byte? Version { get; init; }

	/// <summary>
	/// The dialect of the Mavlink protocol.
	/// </summary>
	public byte? Dialect { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkData"/> record.
	/// </summary>
	/// <param name="enums">The collection of Mavlink enums.</param>
	/// <param name="messages">The collection of Mavlink messages.</param>
	/// <param name="includes">The collection of include directives.</param>
	/// <param name="version">The version of the Mavlink protocol.</param>
	/// <param name="dialect">The dialect of the Mavlink protocol.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enums"/>, <paramref name="messages"/>, or <paramref name="includes"/> is <c>null</c>.</exception>
	public MavlinkData(
		ImmutableArray<MavlinkEnum> enums,
		ImmutableArray<MavlinkMessage> messages,
		ImmutableArray<string> includes,
		byte? version,
		byte? dialect)
	{
		Enums = enums.IsDefault ? throw new ArgumentNullException(nameof(enums)) : enums;
		Messages = messages.IsDefault ? throw new ArgumentNullException(nameof(messages)) : messages;
		Includes = includes.IsDefault ? throw new ArgumentNullException(nameof(includes)) : includes;
		Version = version;
		Dialect = dialect;
	}
}
