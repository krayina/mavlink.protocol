using System.Xml.Linq;

namespace Shmyndra.Mavlink.SourceGenerators.MavlinkGenerator;

public static class MavlinkXmlIncludeOrderer
{
	public static List<string> GetOrderedFiles(Dictionary<string, string> fileContents)
	{
		var orderedFiles = new List<string>();
		var visited = new HashSet<string>();

		void Visit(string file, string basePath)
		{
			if (!visited.Contains(file))
			{
				visited.Add(file);
				var fullPath = Path.Combine(basePath, file);

				var includes = ExtractIncludes(fileContents[fullPath]);
				if (includes.Count == 0)
				{
					// If no includes, add to the beginning of the list
					orderedFiles.Insert(0, fullPath);
				}
				else
				{
					foreach (var include in includes)
					{
						Visit(include, Path.GetDirectoryName(fullPath)!);
					}
					orderedFiles.Add(fullPath);
				}
			}
		}

		foreach (var file in fileContents.Keys)
		{
			Visit(Path.GetFileName(file), Path.GetDirectoryName(file)!);
		}

		return orderedFiles;
	}

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
