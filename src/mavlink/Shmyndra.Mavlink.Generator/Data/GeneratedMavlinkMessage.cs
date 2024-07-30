using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a generated Mavlink message with additional generated fields and namespace.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkMessageTypesGenerator"/> interface
/// and should not be instantiated manually.
/// </remarks>
public record GeneratedMavlinkMessage : MavlinkMessage
{
	/// <summary>
	/// The namespace associated with the generated Mavlink message.
	/// </summary>
	public string GeneratedNamespace { get; init; }

	/// <summary>
	/// The list of generated fields in the Mavlink message.
	/// </summary>
	public ImmutableArray<GeneratedMavlinkMessageField> GeneratedFields { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessage"/> record.
	/// </summary>
	/// <param name="generatedNamespace">The namespace associated with the generated message.</param>
	/// <param name="generatedFields">The list of generated fields for the message.</param>
	/// <param name="originalMessage">The original Mavlink message.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedNamespace"/> or <paramref name="generatedFields"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkMessage(string generatedNamespace, ImmutableArray<GeneratedMavlinkMessageField> generatedFields, MavlinkMessage originalMessage)
		: base(originalMessage)
	{
		GeneratedNamespace = generatedNamespace ?? throw new ArgumentNullException(nameof(generatedNamespace));
		GeneratedFields = generatedFields.IsDefault ? throw new ArgumentNullException(nameof(generatedFields)) : generatedFields;
	}
}
