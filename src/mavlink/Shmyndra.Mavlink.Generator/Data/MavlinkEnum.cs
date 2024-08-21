using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents an enum in Mavlink with a name, description, bitmask, and a list of entries.
/// </summary>
public record MavlinkEnum
{
	/// <summary>
	/// The name of the Mavlink enum.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// The description of the Mavlink enum.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Indicates whether the enum is a bitmask.
	/// </summary>
	public bool? Bitmask { get; init; }

	/// <summary>
	/// The array of entries in the Mavlink enum.
	/// </summary>
	public ImmutableArray<MavlinkEnumEntry> Entries { get; init; }

	/// <summary>
	/// The deprecation information of the Mavlink enum.
	/// </summary>
	public MavlinkDeprecatedInfo? Deprecated { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkEnum"/> record.
	/// </summary>
	/// <param name="name">The name of the Mavlink enum.</param>
	/// <param name="description">The description of the Mavlink enum.</param>
	/// <param name="bitmask">Indicates whether the enum is a bitmask.</param>
	/// <param name="entries">The array of entries in the Mavlink enum.</param>
	/// <param name="deprecated">The deprecation information of the Mavlink enum.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="entries"/> is <c>null</c>.</exception>
	public MavlinkEnum(
		string name,
		string? description,
		bool? bitmask,
		ImmutableArray<MavlinkEnumEntry> entries,
		MavlinkDeprecatedInfo? deprecated)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Entries = entries;
		Description = description;
		Bitmask = bitmask;
		Deprecated = deprecated;
	}
}
