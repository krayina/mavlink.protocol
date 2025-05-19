using System.Collections;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public class ReadOnlyMavlinkTree : IReadOnlyCollection<MavlinkNode>
{
	private readonly ImmutableHashSet<MavlinkNode> _nodes;

	public ReadOnlyMavlinkTree(IEnumerable<MavlinkNode> nodes)
	{
		_nodes = nodes.ToImmutableHashSet();
	}

	public int Count => _nodes.Count;

	public IEnumerator<MavlinkNode> GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

	public void ForEachTree(Action<MavlinkNode> action)
	{
		var visited = new HashSet<MavlinkNode>();

		foreach (var node in _nodes)
		{
			Visit(node, visited, action);
		}
	}

	private void Visit(MavlinkNode node, HashSet<MavlinkNode> visited, Action<MavlinkNode> action)
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
}
