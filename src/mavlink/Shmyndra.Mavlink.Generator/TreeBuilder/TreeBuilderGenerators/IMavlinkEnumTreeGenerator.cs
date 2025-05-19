using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkEnumTreeGenerator
{
	/// <summary>
	/// Generates MAVLink enums for a given node, considering included namespaces from the tree.
	/// </summary>
	/// <param name="node">The MAVLink node containing enum data.</param>
	/// <param name="namespaceName">The namespace for the generated enums.</param>
	/// <returns>A list of generated MAVLink enums.</returns>
	ImmutableArray<GeneratedMavlinkEnum> GenerateEnums(MavlinkNode node, string namespaceName);
}
