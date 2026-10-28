using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents an entry in a Mavlink enum.
/// </summary>
public record MavlinkEnumEntry
{
	/// <summary>
	/// The name of the Mavlink enum entry.
	/// </summary>
	/// <remarks>
	/// Pattern: [\w_]+.
	/// </remarks>
	public string Name { get; init; }

	/// <summary>
	/// The value of the Mavlink enum entry.
	/// </summary>
	/// <remarks>
	/// Pattern: 2**\d{1,2}.
	/// </remarks>
	public uint Value { get; init; }

	/// <summary>
	/// The description of the Mavlink enum entry.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// The detailed information of the Mavlink enum entry.
	/// </summary>
	public ImmutableArray<MavlinkEnumEntryDetail> Details { get; init; }

	/// <summary>
	/// The deprecation information of the Mavlink enum entry.
	/// </summary>
	public MavlinkDeprecatedInfo? Deprecated { get; init; }

	/// <summary>
	/// Indicates whether the entry has a location.
	/// </summary>
	public bool? HasLocation { get; init; }

	/// <summary>
	/// Indicates whether the entry is a destination.
	/// </summary>
	public bool? IsDestination { get; init; }

	/// <summary>
	/// Indicates whether the entry is mission only.
	/// </summary>
	public bool? MissionOnly { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkEnumEntry"/> record.
	/// </summary>
	/// <param name="name">The name of the Mavlink enum entry.</param>
	/// <param name="value">The value of the Mavlink enum entry.</param>
	/// <param name="description">The description of the Mavlink enum entry.</param>
	/// <param name="details">The detailed information of the Mavlink enum entry.</param>
	/// <param name="deprecated">The deprecation information of the Mavlink enum entry.</param>
	/// <param name="hasLocation">Indicates whether the entry has a location.</param>
	/// <param name="isDestination">Indicates whether the entry is a destination.</param>
	/// <param name="missionOnly">Indicates whether the entry is mission only.</param>
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
