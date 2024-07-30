namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents an entry in a generated Mavlink enum with an additional generated name.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkEnumTypesGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkEnumEntry : MavlinkEnumEntry
{
	/// <summary>
	/// The generated name of the Mavlink enum entry.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkEnumEntry"/> record.
	/// </summary>
	/// <param name="generatedName">The generated name of the Mavlink enum entry.</param>
	/// <param name="original">The original Mavlink enum entry.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedName"/> or <paramref name="original"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkEnumEntry(string generatedName, MavlinkEnumEntry original)
		: base(original)
	{
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
	}
}
