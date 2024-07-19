using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink message with an ID, name, description, and a list of fields.
/// </summary>
public record MavlinkMessage
{
	/// <summary>
	/// The ID of the Mavlink message type.
	/// </summary>
	public uint Id { get; init; }

	/// <summary>
	/// The name of the Mavlink message.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// The description of the Mavlink message.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// The list of fields in the Mavlink message.
	/// </summary>
	public ImmutableList<MavlinkMessageField> Fields { get; init; }

	/// <summary>
	/// The deprecation information of the Mavlink message.
	/// </summary>
	public MavlinkDeprecatedInfo? Deprecated { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessage"/> record.
	/// </summary>
	/// <param name="id">The ID of the Mavlink message.</param>
	/// <param name="name">The name of the Mavlink message.</param>
	/// <param name="description">The description of the Mavlink message.</param>
	/// <param name="fields">The list of fields in the Mavlink message.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="fields"/> is <c>null</c>.</exception>
	public MavlinkMessage(
		uint id,
		string name,
		string? description,
		ImmutableList<MavlinkMessageField> fields,
		MavlinkDeprecatedInfo? deprecated)
	{
		Id = id;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Description = description;
		Fields = fields ?? throw new ArgumentNullException(nameof(fields));
		Deprecated = deprecated;
	}
}
