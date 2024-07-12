using System.Xml.Linq;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides functionality to build a tree of MAVLink files based on include dependencies.
/// </summary>
public static class MavlinkFilesTreeBuilder
{
	/// <summary>
	/// Represents a node in the MAVLink files tree.
	/// </summary>
	/// <param name="FilePath">The path to the MAVLink file.</param>
	/// <param name="Includes">The list of included files for this node.</param>
	public record MavlinkFileNode(string FilePath, List<MavlinkFileNode> Includes);

	/// <summary>
	/// Builds a tree of MAVLink files based on their include dependencies.
	/// </summary>
	/// <param name="fileContents">A dictionary with file paths as keys and file contents as values.</param>
	/// <returns>The list of root nodes of the MAVLink files tree.</returns>
	public static List<MavlinkFileNode> Build(Dictionary<string, string> fileContents)
	{
		var nodes = fileContents.Keys.ToDictionary(path => path, path => new MavlinkFileNode(path, new List<MavlinkFileNode>()));

		foreach (var kvp in fileContents)
		{
			var includes = ExtractIncludes(kvp.Value);
			foreach (var include in includes)
			{
				var includePath = Path.Combine(Path.GetDirectoryName(kvp.Key)!, include);
				if (nodes.TryGetValue(includePath, out var includeNode))
				{
					nodes[kvp.Key].Includes.Add(includeNode);
				}
			}
		}

		var rootNodes = nodes.Values.Where(node => !nodes.Values.Any(n => n.Includes.Contains(node))).ToList();
		return rootNodes;
	}

	/// <summary>
	/// Extracts the list of includes from the content of a MAVLink file.
	/// </summary>
	/// <param name="content">The content of the MAVLink file.</param>
	/// <returns>A list of file paths that are included in the MAVLink file.</returns>
	private static List<string> ExtractIncludes(string content)
	{
		var includes = new List<string>();
		var doc = XDocument.Parse(content);
		var includeElements = doc.Descendants("include");

		foreach (var element in includeElements)
		{
			includes.Add(element.Value.Trim());
		}

		return includes;
	}
}
