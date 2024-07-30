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
	/// The namespace of the original Mavlink enum to which this entry belongs.
	/// </summary>
	/// <remarks>
	/// This property is used to indicate the namespace of the original enum from which this entry was generated.
	/// This is particularly important when merging enums from different namespaces into a single <see cref="GeneratedMavlinkEnum"/>,
	/// as it helps to track the origin of each entry.
	/// </remarks>
	public string Namespace { get; init; }

	/// <summary>
	/// The generated name of the Mavlink enum entry.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkEnumEntry"/> record.
	/// </summary>
	/// <param name="namespace">The namespace of the original enum.</param>
	/// <param name="generatedName">The generated name of the Mavlink enum entry.</param>
	/// <param name="original">The original Mavlink enum entry.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/>, <paramref name="generatedName"/>, or <paramref name="original"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkEnumEntry(string @namespace, string generatedName, MavlinkEnumEntry original)
		: base(original)
	{
		Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
	}
}
