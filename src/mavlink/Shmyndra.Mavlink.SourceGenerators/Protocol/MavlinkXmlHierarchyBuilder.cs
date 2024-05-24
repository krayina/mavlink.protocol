using System.Xml.Linq;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal record TreeNode<T>(T Data)
{
	public List<TreeNode<T>> Children { get; } = new List<TreeNode<T>>();

	public void AddChild(TreeNode<T> child) => Children.Add(child);
}

internal static class MavlinkXmlHierarchyBuilder
{
	public static List<TreeNode<string>> Build(Dictionary<string, string> xmlFiles)
	{
		var fileNodes = new Dictionary<string, TreeNode<string>>();
		var includedFilesSet = new HashSet<string>();

		foreach (var kvp in xmlFiles)
		{
			var fileName = kvp.Key;
			var xmlText = kvp.Value;

			if (!fileNodes.ContainsKey(fileName))
			{
				fileNodes[fileName] = new TreeNode<string>(fileName);
			}

			var rootNode = fileNodes[fileName];

			var xmlDoc = XDocument.Parse(xmlText);
			var includedFiles = xmlDoc.Descendants("include")
				.Select(x => x.Value);

			foreach (var includedFile in includedFiles)
			{
				// Resolve relative path
				var includedFilePath = Path.Combine(Path.GetDirectoryName(fileName), includedFile);

				if (!fileNodes.ContainsKey(includedFilePath))
				{
					fileNodes[includedFilePath] = new TreeNode<string>(includedFilePath);
				}

				var includedNode = fileNodes[includedFilePath];
				rootNode.AddChild(includedNode);
				includedFilesSet.Add(includedFilePath);
			}
		}

		return fileNodes.Values.Where(node => !includedFilesSet.Contains(node.Data)).ToList();
	}
}
