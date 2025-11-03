using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkMessageGenerator : IGeneratedStorage<GeneratedMavlinkMessage>
{
	/// <summary>
	/// Generates a C# record declaration for a Mavlink message using Scriban templates.
	/// </summary>
	/// <param name="message">The Mavlink message to be processed.</param>
	/// <param name="namespace">The namespace in which the generated message type will reside.</param>
	/// <param name="generatedEnums">An array of generated Mavlink enums. Can be null if no enums are used.</param>
	/// <returns>The generated Mavlink message, including its declaration string and metadata.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namespace"/> or <paramref name="message"/> is <c>null</c>.</exception>
	/// <exception cref="InvalidOperationException">Thrown when a message with the same name in the specified namespace has already been generated.</exception>
	/// <exception cref="ArgumentException">Thrown when a required enum is not found in the provided <paramref name="generatedEnums"/> array.</exception>
	GeneratedMavlinkMessage GenerateMavlinkMessage(
		MavlinkMessage message,
		string @namespace,
		ImmutableArray<GeneratedMavlinkEnum>? generatedEnums);
}
