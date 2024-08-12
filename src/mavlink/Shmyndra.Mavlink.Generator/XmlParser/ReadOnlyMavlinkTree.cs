using System.Collections;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public class ReadOnlyMavlinkTree : IReadOnlyCollection<MavlinkFileNode>
{
	private readonly ImmutableHashSet<MavlinkFileNode> _nodes;
	private readonly Dictionary<MavlinkFileNode, MavlinkFileNode?> _parentMap;

	public ReadOnlyMavlinkTree(IEnumerable<MavlinkFileNode> nodes)
	{
		_nodes = nodes.ToImmutableHashSet();
		_parentMap = BuildParentMap(_nodes);
	}

	public int Count => _nodes.Count;

	public IEnumerator<MavlinkFileNode> GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

	public MavlinkFileNode? GetParent(MavlinkFileNode node)
	{
		if (_parentMap.TryGetValue(node, out var parentNode))
		{
			return parentNode;
		}
		return null;
	}

	public void ForEachTree(Action<MavlinkFileNode> action)
	{
		var visited = new HashSet<MavlinkFileNode>();

		foreach (var node in _nodes)
		{
			Visit(node, visited, action);
		}
	}

	private void Visit(MavlinkFileNode node, HashSet<MavlinkFileNode> visited, Action<MavlinkFileNode> action)
	{
		if (!visited.Contains(node))
		{
			visited.Add(node);

			foreach (var include in node.Includes)
			{
				Visit(include, visited, action);
			}

			action(node);
		}
	}

	private Dictionary<MavlinkFileNode, MavlinkFileNode?> BuildParentMap(IEnumerable<MavlinkFileNode> nodes)
	{
		var parentMap = new Dictionary<MavlinkFileNode, MavlinkFileNode?>();

		foreach (var node in nodes)
		{
			foreach (var include in node.Includes)
			{
				parentMap[include] = node;
			}

			if (!parentMap.ContainsKey(node))
			{
				// Root node
				parentMap[node] = null;
			}
		}

		return parentMap;
	}
}
