namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a node in the MAVLink files tree.
/// </summary>
/// <param name="FilePath">The path to the MAVLink file.</param>
/// <param name="Data">The parsed MAVLink data for the file.</param>
/// <param name="Includes">The list of included files for this node.</param>
public record MavlinkFileNode(string FilePath, MavlinkData Data, List<MavlinkFileNode> Includes)
{
	/// <summary>
	/// Finds a node in the MAVLink files tree that matches the specified condition.
	/// </summary>
	/// <param name="predicate">The condition to match the node.</param>
	/// <returns>The found node, or null if no matching node is found.</returns>
	public MavlinkFileNode? FindNode(Func<MavlinkFileNode, bool> predicate)
	{
		// Check if the current node matches the condition
		if (predicate(this))
		{
			return this;
		}

		// Recursively search in the included nodes
		foreach (var include in Includes)
		{
			var found = include.FindNode(predicate);
			if (found != null)
			{
				return found;
			}
		}

		// Return null if no matching node is found
		return null;
	}
}

/// <summary>
/// Provides functionality to build a tree of MAVLink files based on include dependencies and parse them into MavlinkData.
/// </summary>
public interface IMavlinkFilesTreeBuilder
{
	ReadOnlyMavlinkTree Build(IReadOnlyDictionary<string, string> fileContents);
}

public class MavlinkFilesTreeBuilder : IMavlinkFilesTreeBuilder
{
	private readonly IMavlinkParser _parser;

	public MavlinkFilesTreeBuilder(IMavlinkParser parser)
	{
		_parser = parser;
	}

	/// <summary>
	/// Builds a tree of MAVLink files based on their include dependencies and parses them into MavlinkData.
	/// </summary>
	/// <param name="fileContents">A dictionary with file paths as keys and file contents as values.</param>
	/// <returns>The list of root nodes of the MAVLink files tree.</returns>
	public ReadOnlyMavlinkTree Build(IReadOnlyDictionary<string, string> fileContents)
	{
		// Create nodes with parsed MavlinkData for each file
		var nodes = fileContents.ToDictionary(
			kvp => kvp.Key,
			kvp => new MavlinkFileNode(kvp.Key, _parser.Parse(kvp.Value), new List<MavlinkFileNode>())
		);

		// Populate the include dependencies for each node
		foreach (var kvp in fileContents)
		{
			var node = nodes[kvp.Key];
			foreach (var include in node.Data.Includes)
			{
				var includePath = Path.Combine(Path.GetDirectoryName(kvp.Key)!, include);
				if (nodes.TryGetValue(includePath, out var includeNode))
				{
					node.Includes.Add(includeNode);
				}
			}
		}

		// Identify and return the root nodes (nodes not included by any other node)
		var tree = nodes.Values.Where(node => !nodes.Values.Any(n => n.Includes.Contains(node)));
		return new ReadOnlyMavlinkTree(tree);
	}
}
