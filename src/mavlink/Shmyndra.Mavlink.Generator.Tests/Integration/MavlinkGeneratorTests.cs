using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator.Tests.Integration;

public class MavlinkGeneratorTests
{
	private const string SNAPSHOT_PATH = "..\\Snapshots/Integration/MavlinkGeneratorTests";

	[Fact]
	public async Task MavlinkGenerator_Verify()
	{
		// Arrange
		IMavlinkParser mavlinkParser = new MavlinkXmlParser();
		IMavlinkFilesTreeBuilder filesTreeBuilder = new MavlinkFilesTreeBuilder(mavlinkParser);
		IMavlinkEnumGenerator enumGenerator = new MavlinkEnumGenerator();
		IMavlinkMessageGenerator messageGenerator = new MavlinkMessageGenerator(
			new MavlinkMessageBufferDeserializationMethodGenerator(),
			new MavlinkMessageSpanSerializationMethodGenerator()
		);
		IMavlinkSpecificationGenerator specificationGenerator = new MavlinkSpecificationGenerator();

		var generator = new MavlinkGenerator(filesTreeBuilder, enumGenerator, messageGenerator, specificationGenerator);

		var additional = TestsHelper.GetAdditionalTextList([
			"Stubs\\test-mavlink-common.xml",
			"Stubs\\test-mavlink-minimal.xml",
			"Stubs\\test-mavlink-standard.xml"
		]);

		var fileContents = additional.ToImmutableDictionary(key => key.Path, value => value.GetText()!.ToString());

		// Act
		var generatedFiles = generator.GenerateMavlink(fileContents);

		// Assert
		Assert.NotEmpty(generatedFiles);

		var minimalFile = generatedFiles.First().Value;
		Assert.Contains("MavType", minimalFile.Syntax.ToFullString());

		await Verify(string.Join("\n", generatedFiles.Values.Select(x => x.Syntax.ToNormalizedString())))
			.UseDirectory(SNAPSHOT_PATH);
	}
#if false

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

	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateEnumWithDependenciesToOtherEnums_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <include>testThirdFile.xml</include>
  <include>testSecondFile.xml</include>
  <enums>
    <enum name=""MAV_CMD"">
      <entry name=""MAV_CMD_PRS_SET_ARM"" value=""65536"" isDestination=""false"" hasLocation=""false"" />
    </enum>
  </enums>
</mavlink>"),
			new TestAdditionalFile("testSecondFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""MAV_CMD"">
      <entry name=""MAV_CMD_Second_Test"" value=""60040"" isDestination=""false"" hasLocation=""false"" />
    </enum>
  </enums>
</mavlink>"),
			new TestAdditionalFile("testThirdFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""MAV_CMD"">
      <entry name=""MAV_CMD_Third_Test"" value=""6020"" isDestination=""false"" hasLocation=""false"" />
    </enum>
  </enums>
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
	public Task MavlinkIncrementalGenerator_GenerateMessageWithDeserializeMethod_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <include>testSecondFile.xml</include>
  <enums>
    <enum name=""MAV_CMD"">
      <entry name=""MAV_CMD_PRS_SET_ARM"" value=""65536"" isDestination=""false"" hasLocation=""false"" />
    </enum>
  </enums>
  <messages>
    <message id=""22"" name=""TEST_MESSAGE"">
      <field type=""int8_t"" name=""SomeFirstProperty"" />
      <field type=""char[230]"" name=""SomeSecondProperty"" />
      <field type=""uint8_t"" name=""SomeThirdProperty"" enum=""MAV_CMD"" />
      <field type=""uint8_t"" name=""SomeFourthProperty"" enum=""ENUM_FROM_ANOTHER_FILE"" />
    </message>
  </messages>
</mavlink>"),
			new TestAdditionalFile("testSecondFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""ENUM_FROM_ANOTHER_FILE"">
      <entry name=""MAV_CMD_Second_Test"" value=""60040"" isDestination=""false"" hasLocation=""false"" />
    </enum>
  </enums>
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
	public Task MavlinkIncrementalGenerator_GenerateEnumWithBitmask_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""MAV_CMD"" bitmask=""true"">
      <entry name=""MAV_CMD_PRS_SET_ARM"" value=""1"" />
    </enum>
  </enums>
</mavlink>"));

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}

	[Fact]
	public Task MavlinkIncrementalGenerator_GenerateDisplayBitmaskArrayField_Verify()
	{
		// arrange
		var generator = new MavlinkIncrementalGenerator();

		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("testFile.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""ESC_FAILURE_FLAGS"" bitmask=""true"">
      <description>Flags to report ESC failures.</description>
      <entry value=""0"" name=""ESC_FAILURE_NONE"">
        <description>No ESC failure.</description>
      </entry>
      <entry value=""1"" name=""ESC_FAILURE_OVER_CURRENT"">
        <description>Over current failure.</description>
      </entry>
      <entry value=""2"" name=""ESC_FAILURE_OVER_VOLTAGE"">
        <description>Over voltage failure.</description>
      </entry>
      <entry value=""4"" name=""ESC_FAILURE_OVER_TEMPERATURE"">
        <description>Over temperature failure.</description>
      </entry>
      <entry value=""8"" name=""ESC_FAILURE_OVER_RPM"">
        <description>Over RPM failure.</description>
      </entry>
      <entry value=""16"" name=""ESC_FAILURE_INCONSISTENT_CMD"">
        <description>Inconsistent command failure i.e. out of bounds.</description>
      </entry>
      <entry value=""32"" name=""ESC_FAILURE_MOTOR_STUCK"">
        <description>Motor stuck failure.</description>
      </entry>
      <entry value=""64"" name=""ESC_FAILURE_GENERIC"">
        <description>Generic ESC failure.</description>
      </entry>
    </enum>
  </enums>
  <messages>
    <message id=""290"" name=""ESC_INFO"">
      <field type=""uint8_t"" name=""info"" display=""bitmask"">Information regarding online/offline status of each ESC.</field>
      <field type=""uint16_t[4]"" name=""failure_flags"" enum=""ESC_FAILURE_FLAGS"" display=""bitmask"">Bitmap of ESC failure flags.</field>
      <field type=""uint32_t[4]"" name=""error_count"">Number of reported errors by each ESC since boot.</field>
    </message>
  </messages>
</mavlink>"));

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}
#endif
}
