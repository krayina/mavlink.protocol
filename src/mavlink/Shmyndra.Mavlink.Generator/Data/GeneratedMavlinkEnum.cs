using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a generated Mavlink enum with additional metadata.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkEnumTypesGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkEnum : MavlinkEnum
{
	/// <summary>
	/// The namespace associated with the generated Mavlink enum.
	/// </summary>
	public string Namespace { get; init; }

	/// <summary>
	/// The list of generated entries in the Mavlink enum.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkEnumEntry> GeneratedEntries { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkEnum"/> class.
	/// </summary>
	/// <param name="namespace">The namespace associated with the generated enum.</param>
	/// <param name="generatedEntries">The array of generated entries.</param>
	/// <param name="original">The original Mavlink enum.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/> or <paramref name="generatedEntries"/> is null.</exception>
	internal GeneratedMavlinkEnum(string @namespace, ImmutableArray<GeneratedMavlinkEnumEntry> generatedEntries, MavlinkEnum original)
		: base(original)
	{
		Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
		GeneratedEntries = generatedEntries;
	}
}
