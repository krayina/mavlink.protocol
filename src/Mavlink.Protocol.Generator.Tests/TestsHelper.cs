using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator.Tests;

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

	public static GeneratorDriver RunSourceGeneratorDriver(
		this ISourceGenerator generator,
		ImmutableArray<AdditionalText>? additional = null,
		IEnumerable<PortableExecutableReference>? references = null)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName: "Shmyndra.Generator",
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

	public static CSharpGeneratorDriver RunIncrementalGeneratorDriver(
	this IIncrementalGenerator generator,
	ImmutableArray<AdditionalText>? additional = null,
	IEnumerable<PortableExecutableReference>? references = null,
	params SyntaxTree[] syntaxTrees)
	{
		var referencesList = new List<PortableExecutableReference>(references ?? Enumerable.Empty<PortableExecutableReference>())
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
		};

		var compilation = CSharpCompilation.Create(
			assemblyName: "Shmyndra.IIncrementalGenerators",
			syntaxTrees: syntaxTrees,
			references: referencesList,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var driver = CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { generator });
		if (additional != null)
		{
			driver = (CSharpGeneratorDriver)driver.AddAdditionalTexts(additional.Value);
		}

		return (CSharpGeneratorDriver)driver.RunGenerators(compilation);
	}
}
