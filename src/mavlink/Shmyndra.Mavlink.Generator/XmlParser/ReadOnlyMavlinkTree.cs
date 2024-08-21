using System.Collections;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public class ReadOnlyMavlinkTree : IReadOnlyCollection<MavlinkFileNode>
{
	private readonly ImmutableHashSet<MavlinkFileNode> _nodes;

	public ReadOnlyMavlinkTree(IEnumerable<MavlinkFileNode> nodes)
	{
		_nodes = nodes.ToImmutableHashSet();
	}

	public int Count => _nodes.Count;

	public IEnumerator<MavlinkFileNode> GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();

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
}
