using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Shmyndra.Mavlink.SourceGenerators.MavlinkGenerator;
using Shmyndra.Mavlink.SourceGenerators.MavlinkCachedMessageTypesGenerator;

namespace Shmyndra.Mavlink.SourceGenerators.Tests.Unit;

public class MavlinkCachedMessageTypesSourceGeneratorTests
{
	[Fact]
	public async Task MavlinkCachedMessageTypesSourceGenerator_GenerateCachedDictionaryWithTwoTypes_Verify()
	{
		// arrange
		var cachedIdentifiersGenerator = new MavlinkCachedMessageTypesSourceGenerator();
		var mavlinkGenerator = new MavlinkIncrementalGenerator();

		var additionalFiles = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <messages>
    <message id=""22"" name=""TEST_FIRST_MESSAGE"">
      <field type=""int8_t"" name=""SomeProperty"" />
    </message>
    <message id=""55"" name=""TEST_SECOND_MESSAGE"">
      <field type=""int8_t"" name=""SomeProperty"" />
    </message>
  </messages>
</mavlink>")
		);

		// act
		var mavlinkGeneratorDriver = CSharpGeneratorDriver.Create(mavlinkGenerator)
			.AddAdditionalTexts(additionalFiles)
			.RunGeneratorsAndUpdateCompilation(CSharpCompilation.Create("test"), out var updatedCompilation, out var diagnostics);

		var cachedIdentifiersGeneratorDriver = CSharpGeneratorDriver.Create(cachedIdentifiersGenerator)
			.RunGeneratorsAndUpdateCompilation(updatedCompilation, out var finalCompilation, out diagnostics);

		var mavlinkGeneratorResult = mavlinkGeneratorDriver.GetRunResult();
		var cachedIdentifiersGeneratorResult = cachedIdentifiersGeneratorDriver.GetRunResult().Results.Single();

		var generatedCode = string.Join(Environment.NewLine, cachedIdentifiersGeneratorResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		await Verify(generatedCode).UseDirectory("..\\Snapshots");
	}
}
