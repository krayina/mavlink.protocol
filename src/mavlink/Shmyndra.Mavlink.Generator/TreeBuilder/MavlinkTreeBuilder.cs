namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a node in the MAVLink dependency tree.
/// </summary>
/// <param name="Namespace">The namespace associated with the MAVLink data.</param>
/// <param name="Data">The parsed MAVLink data for the node.</param>
/// <param name="Includes">The list of included nodes for this node.</param>
public record MavlinkNode(string Namespace, MavlinkData Data, List<MavlinkNode> Includes)
{
	/// <summary>
	/// Finds a node in the MAVLink dependency tree that matches the specified condition.
	/// </summary>
	/// <param name="predicate">The condition to match the node.</param>
	/// <returns>The found node, or null if no matching node is found.</returns>
	public MavlinkNode? FindNode(Func<MavlinkNode, bool> predicate)
	{
		if (predicate(this))
		{
			return this;
		}

		foreach (var include in Includes)
		{
			var found = include.FindNode(predicate);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}

/// <summary>
/// Defines a contract for building a tree of MAVLink nodes based on include dependencies.
/// </summary>
public interface IMavlinkTreeBuilder
{
	/// <summary>
	/// Builds a tree of MAVLink nodes from the provided contents.
	/// </summary>
	/// <param name="contents">A dictionary with namespaces as keys and MAVLink contents as values.</param>
	/// <returns>A read-only tree of MAVLink nodes.</returns>
	ReadOnlyMavlinkTree Build(IReadOnlyDictionary<string, string> contents);
}

/// <summary>
/// Builds a tree of MAVLink nodes based on include dependencies and parsed MAVLink data.
/// </summary>
public class MavlinkTreeBuilder : IMavlinkTreeBuilder
{
	private readonly IMavlinkParser _parser;

	/// <summary>
	/// Initializes a new instance of the MavlinkTreeBuilder with the specified parser.
	/// </summary>
	/// <param name="parser">The parser used to process MAVLink contents.</param>
	public MavlinkTreeBuilder(IMavlinkParser parser)
	{
		_parser = parser;
	}

	/// <summary>
	/// Builds a tree of MAVLink nodes from the provided contents.
	/// </summary>
	/// <param name="contents">A dictionary with namespaces as keys and MAVLink contents as values.</param>
	/// <returns>A read-only tree of MAVLink nodes.</returns>
	public ReadOnlyMavlinkTree Build(IReadOnlyDictionary<string, string> contents)
	{
		var nodes = contents.ToDictionary(
			kvp => kvp.Key,
			kvp => new MavlinkNode(kvp.Key, _parser.Parse(kvp.Value), new List<MavlinkNode>())
		);

		foreach (var kvp in contents)
		{
			var node = nodes[kvp.Key];
			foreach (var include in node.Data.Includes)
			{
				if (nodes.TryGetValue(include, out var includeNode))
				{
					node.Includes.Add(includeNode);
				}
			}
		}

		var tree = nodes.Values.Where(node => !nodes.Values.Any(n => n.Includes.Contains(node)));
		return new ReadOnlyMavlinkTree(tree);
	}
}
