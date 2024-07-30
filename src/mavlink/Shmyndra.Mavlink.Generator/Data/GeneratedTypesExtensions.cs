using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides extension methods for converting Mavlink message fields to their generated counterparts.
/// </summary>
internal static class GeneratedTypesExtensions
{
	/// <summary>
	/// Converts a <see cref="MavlinkMessageField"/> to a <see cref="GeneratedMavlinkMessageField"/> with a specified generated name.
	/// </summary>
	/// <param name="field">The original Mavlink message field.</param>
	/// <param name="generatedName">The generated name for the new field.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkMessageField"/> with the specified generated name.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="field"/> or <paramref name="generatedName"/> is <c>null</c>.</exception>
	public static GeneratedMavlinkMessageField ToGeneratedMavlinkMessageField(this MavlinkMessageField field, string generatedName)
	{
		return new GeneratedMavlinkMessageField(generatedName, field);
	}

	/// <summary>
	/// Converts a <see cref="MavlinkEnumEntry"/> to a <see cref="GeneratedMavlinkEnumEntry"/> with a specified generated name and namespace.
	/// </summary>
	/// <param name="entry">The original Mavlink enum entry.</param>
	/// <param name="namespace">The namespace of the original enum to which this entry belongs.</param>
	/// <param name="generatedName">The generated name for the new entry.</param>
	/// <returns>A new <see cref="GeneratedMavlinkEnumEntry"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/> or <paramref name="generatedName"/> is <c>null</c>.</exception>
	public static GeneratedMavlinkEnumEntry ToGeneratedMavlinkEnumEntry(this MavlinkEnumEntry entry, string @namespace, string generatedName)
	{
		return new GeneratedMavlinkEnumEntry(@namespace, generatedName, entry);
	}

	/// <summary>
	/// Converts a <see cref="MavlinkEnum"/> to a <see cref="GeneratedMavlinkEnum"/> with a specified namespace and generated entries.
	/// </summary>
	/// <param name="mavlinkEnum">The original Mavlink enum.</param>
	/// <param name="namespace">The namespace for the generated enum.</param>
	/// <param name="generatedEntries">The list of generated entries to include in the new enum.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkEnum"/> with the specified namespace and entries.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mavlinkEnum"/>, <paramref name="namespace"/>, or <paramref name="generatedEntries"/> is null.</exception>
	public static GeneratedMavlinkEnum ToGeneratedMavlinkEnum(this MavlinkEnum mavlinkEnum, string @namespace, ImmutableList<GeneratedMavlinkEnumEntry> generatedEntries)
	{
		return new GeneratedMavlinkEnum(@namespace, generatedEntries, mavlinkEnum);
	}

	/// <summary>
	/// Converts a <see cref="MavlinkMessage"/> to a <see cref="GeneratedMavlinkMessage"/> with a specified namespace and generated fields.
	/// </summary>
	/// <param name="message">The original Mavlink message.</param>
	/// <param name="generatedNamespace">The namespace for the generated message.</param>
	/// <param name="generatedFields">The list of generated fields to include in the new message.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkMessage"/> with the specified namespace and fields.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/>, <paramref name="generatedNamespace"/>, or <paramref name="generatedFields"/> is null.</exception>
	public static GeneratedMavlinkMessage ToGeneratedMavlinkMessage(this MavlinkMessage message, string generatedNamespace, ImmutableArray<GeneratedMavlinkMessageField> generatedFields)
	{
		return new GeneratedMavlinkMessage(generatedNamespace, generatedFields, message);
	}
}
