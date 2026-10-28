using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

public interface IMavlinkEnumGenerator : IGeneratedStorage<GeneratedMavlinkEnum>
{
	/// <summary>
	/// Generates a new MAVLink enum.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate.</param>
	/// <param name="namespace">The target namespace where the enum will be generated.</param>
	/// <returns>The generated MAVLink enum as a <see cref="GeneratedMavlinkEnum"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enum"/> or <paramref name="namespace"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when an enum with the same name already exists in the specified namespace, or when the enum data is invalid and cannot be processed.</exception>
	/// <remarks>
	/// This method creates a new MAVLink enum based on the provided data and caches it to prevent duplicates. It does not perform merging with other enums.
	/// </remarks>
	GeneratedMavlinkEnum GenerateMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace);

	/// <summary>
	/// Generates a new MAVLink enum and merges it with specified existing enums.
	/// </summary>
	/// <param name="enum">The MAVLink enum data to generate.</param>
	/// <param name="namespace">The target namespace where the enum will be generated.</param>
	/// <param name="existingEnums">An immutable params-array of existing enums to merge with.</param>
	/// <returns>The generated and merged MAVLink enum as a <see cref="GeneratedMavlinkEnum"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="enum"/>, <paramref name="namespace"/>, or <paramref name="existingEnums"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when an enum with the same name already exists in the specified namespace, or when the enum data or merged entries are invalid and cannot be processed.</exception>
	/// <remarks>
	/// This method generates a new MAVLink enum and merges it with the provided existing enums, combining their entries into a single enum. The resulting enum is cached to prevent duplicates.
	/// </remarks>
	GeneratedMavlinkEnum GenerateAndMergeMavlinkEnum(
		MavlinkEnum @enum,
		string @namespace,
		params ImmutableArray<GeneratedMavlinkEnum> existingEnums);
}
