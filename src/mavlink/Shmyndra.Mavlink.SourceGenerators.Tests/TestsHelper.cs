using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.SourceGenerators.Tests;

public static class TestsHelper
{
	public static AdditionalText GetAdditionalText(string path)
	{
		var filePath = $"{Environment.CurrentDirectory}\\{path}";
		var fileText = File.ReadAllText(filePath);
		return new TestAdditionalFile(filePath, fileText);
	}

	public static ImmutableArray<AdditionalText> GetAdditionalTextList(string[] paths)
	{
		var additional = new List<AdditionalText>();

		foreach (var path in paths)
		{
			additional.Add(GetAdditionalText(path));
		}
		return additional.ToImmutableArray();
	}

	public static GeneratorDriver RunGeneratorDriver(
		this ISourceGenerator generator,
		ImmutableArray<AdditionalText>? additional = null,
		IEnumerable<PortableExecutableReference>? references = null)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName: "UIMarkup.Protocol.SourceGenerators",
			references: references);

		var driver = CSharpGeneratorDriver.Create(generator);
		if (additional != null)
		{
			return driver
				.AddAdditionalTexts((ImmutableArray<AdditionalText>)additional)
				.RunGenerators(compilation);
		}
		return driver.RunGenerators(compilation);
	}
}
