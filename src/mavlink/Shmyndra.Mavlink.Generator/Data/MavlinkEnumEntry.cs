using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink enumeration entry.
/// </summary>
public record MavlinkEnumEntry
{
	/// <summary>
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// <para xml:lang="en">The name of the Mavlink enumeration entry.</para>
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// <para xml:lang="en">Pattern: 2\*\*\d{1,2}.</para>
	/// <para xml:lang="en">The value of the Mavlink enumeration entry.</para>
	/// </summary>
	public uint Value { get; init; }

	/// <summary>
	/// <para xml:lang="en">The description of the Mavlink enumeration entry.</para>
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// <para xml:lang="en">The detailed information of the Mavlink enumeration entry.</para>
	/// </summary>
	public ImmutableArray<MavlinkEnumEntryDetail> Details { get; init; }

	/// <summary>
	/// <para xml:lang="en">The deprecation information of the Mavlink enumeration entry.</para>
	/// </summary>
	public MavlinkDeprecatedInfo? Deprecated { get; init; }

	/// <summary>
	/// <para xml:lang="en">Indicates whether the entry has a location.</para>
	/// </summary>
	public bool? HasLocation { get; init; }

	/// <summary>
	/// <para xml:lang="en">Indicates whether the entry is a destination.</para>
	/// </summary>
	public bool? IsDestination { get; init; }

	/// <summary>
	/// <para xml:lang="en">Indicates whether the entry is mission only.</para>
	/// </summary>
	public bool? MissionOnly { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkEnumEntry"/> record.
	/// </summary>
	/// <param name="name">
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// <para xml:lang="en">The name of the Mavlink enumeration entry.</para>
	/// </param>
	/// <param name="value">
	/// <para xml:lang="en">Pattern: 2\*\*\d{1,2}.</para>
	/// <para xml:lang="en">The value of the Mavlink enumeration entry.</para>
	/// </param>
	/// <param name="description">
	/// <para xml:lang="en">The description of the Mavlink enumeration entry.</para>
	/// </param>
	/// <param name="details">
	/// <para xml:lang="en">The detailed information of the Mavlink enumeration entry.</para>
	/// </param>
	/// <param name="deprecated">
	/// <para xml:lang="en">The deprecation information of the Mavlink enumeration entry.</para>
	/// </param>
	/// <param name="hasLocation">
	/// <para xml:lang="en">Indicates whether the entry has a location.</para>
	/// </param>
	/// <param name="isDestination">
	/// <para xml:lang="en">Indicates whether the entry is a destination.</para>
	/// </param>
	/// <param name="missionOnly">
	/// <para xml:lang="en">Indicates whether the entry is mission only.</para>
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="details"/> is <c>null</c>.</exception>
	public MavlinkEnumEntry(
		string name,
		uint value,
		string? description,
		ImmutableArray<MavlinkEnumEntryDetail> details,
		MavlinkDeprecatedInfo? deprecated,
		bool? hasLocation,
		bool? isDestination,
		bool? missionOnly)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Details = details.IsDefault ? throw new ArgumentNullException(nameof(details)) : details;
		Value = value;
		Description = description;
		Deprecated = deprecated;
		HasLocation = hasLocation;
		IsDestination = isDestination;
		MissionOnly = missionOnly;
	}
}

