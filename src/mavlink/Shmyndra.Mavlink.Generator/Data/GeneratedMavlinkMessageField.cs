namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink message field that has been generated with an additional generated name.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkMessageTypesGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkMessageField : MavlinkMessageField
{
	/// <summary>
	/// The generated name of the field.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageField"/> record.
	/// </summary>
	/// <param name="generatedName">The generated name of the field.</param>
	/// <param name="original">The original Mavlink message field.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedName"/> or <paramref name="original"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkMessageField(string generatedName, MavlinkMessageField original)
		: base(original)
	{
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
	}
}
