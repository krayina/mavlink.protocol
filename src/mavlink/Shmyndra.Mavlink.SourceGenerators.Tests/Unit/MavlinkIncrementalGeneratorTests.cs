using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Shmyndra.Mavlink.SourceGenerators.MavlinkGenerator;

namespace Shmyndra.Mavlink.SourceGenerators.Tests.Unit;

public class MavlinkIncrementalGeneratorTests
{
	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateAllTypes_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = TestsHelper.GetAdditionalTextList([
			"Stubs\\test-mavlink-common.xml",
			"Stubs\\test-mavlink-third-empty-include.xml",
			"Stubs\\test-mavlink-minimal.xml",
			"Stubs\\test-mavlink-standard.xml",
			"Stubs\\test-mavlink-second-empty-include.xml"
		]);

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}

	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateTypeWithFieldWhichDependsOnOtherFile_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testCommonFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""TEST_ENUM"">
      <entry value=""0"" name=""TestEnumValue""/>
    </enum>
  </enums>
</mavlink>"),
			new TestAdditionalFile("testSecondFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <include>testCommonFile.xml</include>
  <messages>
    <message id=""0"" name=""TEST_MESSAGE"">
      <field type=""uint8_t"" name=""test"" enum=""TEST_ENUM""/>
    </message>
  </messages>
</mavlink>")
		);

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}

	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateTypeWithArray_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <messages>
    <message id=""0"" name=""TEST_MESSAGE"">
      <field type=""char[230]"" name=""uri"" />
    </message>
  </messages>
</mavlink>")
		);

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}

	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateMessageWithMavlinkIdentifiedTypeAttribute_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <messages>
    <message id=""22"" name=""TEST_MESSAGE"">
      <field type=""int8_t"" name=""SomeProperty"" />
    </message>
  </messages>
</mavlink>")
		);

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}
}
