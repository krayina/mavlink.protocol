using System.Collections.Immutable;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;

public class MavlinkXmlParserTests
{
	[Fact]
	public void ParseEscInfoMessageFromXml_ShouldDeserializeCorrectly()
	{
		// Arrange
		var xmlPath = "Stubs\\test-mavlink-ESC_INFO.xml";
		var additionalText = TestsHelper.GetAdditionalText(xmlPath);
		var xmlContent = additionalText.GetText()!.ToString();

		XmlSerializer serializer = new XmlSerializer(typeof(Mavlink));

		// Act
		Mavlink? mavlink;
		using (StringReader reader = new StringReader(xmlContent ?? string.Empty))
		{
			mavlink = serializer.Deserialize(reader) as Mavlink;
		}

		// Assert
		Assert.NotNull(mavlink);
		Assert.Single(mavlink.Messages);
		var escInfoMessage = mavlink.Messages.FirstOrDefault(m => m.Name == "ESC_INFO");
		Assert.NotNull(escInfoMessage);
		Assert.Equal(290U, escInfoMessage.Id);
		Assert.Equal("ESC_INFO", escInfoMessage.Name);
		Assert.Equal("ESC information for lower rate streaming. Recommended streaming rate 1Hz. See ESC_STATUS for higher-rate ESC data.", escInfoMessage.Description);

		Assert.Equal(9, escInfoMessage.Field.Count);

		foreach (var field in escInfoMessage.Field)
		{
			System.Diagnostics.Debug.WriteLine($"Name: {field.Name}, Type: {field.Type}, Enum: {field.Enum}, Display: {field.Display}, Units: {field.Units}");
		}

		Assert.Contains(escInfoMessage.Field, f => f.Name == "index" && f.Type == "uint8_t" && f.Instance && f.MinValue == 0 && f.MaxValue == 0 && f.Increment == 0 && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "time_usec" && f.Type == "uint64_t" && f.Units == SiUnit.Us);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "counter" && f.Type == "uint16_t" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "count" && f.Type == "uint8_t" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "connection_type" && f.Type == "uint8_t" && f.Enum == "ESC_CONNECTION_TYPE" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "info" && f.Type == "uint8_t" && f.Display == "bitmask" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "failure_flags" && f.Type == "uint16_t[4]" && f.Enum == "ESC_FAILURE_FLAGS" && f.Display == "bitmask" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "error_count" && f.Type == "uint32_t[4]" && f.Units == SiUnit.S);
		Assert.Contains(escInfoMessage.Field, f => f.Name == "temperature" && f.Type == "int16_t[4]" && f.Units == SiUnit.CdegC);
	}

	[Fact]
	public void Parse_ValidXmlContent_ShouldReturnMavlinkData()
	{
		// Arrange
		var xmlPath = "Stubs\\test-mavlink-ESC_INFO.xml";
		var additionalText = TestsHelper.GetAdditionalText(xmlPath);
		var xmlContent = additionalText.GetText()!.ToString();

		var parser = new MavlinkXmlParser();

		// Act
		var mavlinkData = parser.Parse(xmlContent);

		// Assert
		Assert.NotNull(mavlinkData);
		Assert.NotEmpty(mavlinkData.Enums);
		Assert.NotEmpty(mavlinkData.Messages);

		var escFailureFlagsEnum = mavlinkData.Enums.FirstOrDefault(e => e.Name == "ESC_FAILURE_FLAGS");
		Assert.NotNull(escFailureFlagsEnum);
		Assert.True(escFailureFlagsEnum.Bitmask);
		Assert.Equal("Flags to report ESC failures.", escFailureFlagsEnum.Description);
		Assert.Equal(8, escFailureFlagsEnum.Entries.Count);
		Assert.Contains(escFailureFlagsEnum.Entries, entry => entry.Name == "ESC_FAILURE_NONE" && entry.Value == 0);
		Assert.Contains(escFailureFlagsEnum.Entries, entry => entry.Name == "ESC_FAILURE_OVER_CURRENT" && entry.Value == 1);

		var escInfoMessage = mavlinkData.Messages.FirstOrDefault(m => m.Name == "ESC_INFO");
		Assert.NotNull(escInfoMessage);
		Assert.Equal(290U, escInfoMessage.Id);
		Assert.Equal("ESC_INFO", escInfoMessage.Name);
		Assert.Equal("ESC information for lower rate streaming. Recommended streaming rate 1Hz. See ESC_STATUS for higher-rate ESC data.", escInfoMessage.Description);
		Assert.NotEmpty(escInfoMessage.Fields);
		Assert.Contains(escInfoMessage.Fields, f => f.Name == "index" && f.Type.TypeName == "uint8_t" && f.Instance!.Value);
	}

	[Fact]
	public void Parse_ValidXmlContent_ShouldReturnMavlinkDataWith3RequiredAnd2NonRequiredFields()
	{
		// Arrange
		var xmlAdditionalFile = new TestAdditionalFile("test.xml", @"<?xml version=""1.0""?>
<mavlink>
  <messages>
    <message id=""44"" name=""MISSION_COUNT"">
      <field type=""uint8_t"" name=""target_system"" />
      <field type=""uint8_t"" name=""target_component"" />
      <field type=""uint16_t"" name=""count"" />
      <extensions/>
      <field type=""uint8_t"" name=""mission_type"" />
      <field type=""uint32_t"" name=""opaque_id"" invalid=""0"" />
    </message>
  </messages>
</mavlink>");

		// Act
		var parser = new MavlinkXmlParser();
		var mavlinkData = parser.Parse(xmlAdditionalFile.GetText()!.ToString());

		// Assert
		Assert.Single(mavlinkData.Messages);

		var message = mavlinkData.Messages.First();
		var fields = message.Fields;

		Assert.Equal(5, fields.Count);

		var requiredFields = fields.Count(f => f.IsRequired);
		var nonRequiredFields = fields.Count(f => !f.IsRequired);

		Assert.Equal(3, requiredFields);
		Assert.Equal(2, nonRequiredFields);
	}

	[Fact]
	public void Parse_ValidXmlContentWithFourEnums_ShouldReturnMavlinkDataWithSortedEnums()
	{
		// Arrange
		var xmlAdditionalFile = new TestAdditionalFile("test.xml", @"<?xml version=""1.0""?>
<mavlink>
  <enums>
    <enum name=""THIS_ENUM_SHOULD_BE_THIRD"">
      <entry value=""0"" name=""TEST_ENTRY_AA"">
        <param index=""1"" label=""Test label"" enum=""THIS_ENUM_SHOULD_BE_SECOND"" />
      </entry>
    </enum>
    <enum name=""THIS_ENUM_SHOULD_BE_FOURTH"">
      <entry value=""1"" name=""TEST_ENTRY_AA"">
        <param index=""1"" label=""Test label"" enum=""ENUM_FROM_ANOTHER_FILE"" />
      </entry>
    </enum>
    <enum name=""THIS_ENUM_SHOULD_BE_SECOND"">
      <entry value=""2"" name=""TEST_ENTRY"">
        <param index=""1"" label=""Test label"" enum=""THIS_ENUM_SHOULD_BE_FIRST"" />
      </entry>
    </enum>
    <enum name=""THIS_ENUM_SHOULD_BE_FIRST"" />
  </enums>
</mavlink>");

		// Act
		var parser = new MavlinkXmlParser();
		var mavlinkData = parser.Parse(xmlAdditionalFile.GetText()!.ToString());

		// Assert
		Assert.Equal(4, mavlinkData.Enums.Length);
		Assert.Equal("THIS_ENUM_SHOULD_BE_FIRST", mavlinkData.Enums[0].Name);
		Assert.Equal("THIS_ENUM_SHOULD_BE_SECOND", mavlinkData.Enums[1].Name);
		Assert.Equal("THIS_ENUM_SHOULD_BE_THIRD", mavlinkData.Enums[2].Name);
		Assert.Equal("THIS_ENUM_SHOULD_BE_FOURTH", mavlinkData.Enums[3].Name);
	}

	[Fact]
	public void GetMavlinkFileNodes_ValidXmlFilesWithIncludes_ShouldReturnTwoRootNodes()
	{
		// Arrange
		var additional = ImmutableArray.Create<AdditionalText>(
			new TestAdditionalFile("ThisFileShouldBeFourth.xml", @"<?xml version=""1.0""?>
<mavlink>
  <include>ThisFileShouldBeThird.xml</include>
</mavlink>"),
			new TestAdditionalFile("ThisFileShouldBeSecond.xml", @"<?xml version=""1.0""?>
<mavlink />"),
			new TestAdditionalFile("ThisFileShouldBeThird.xml", @"<?xml version=""1.0""?>
<mavlink>
  <include>ThisFileShouldBeSecond.xml</include>
</mavlink>"),
			new TestAdditionalFile("ThisFileShouldBeFirst.xml", @"<?xml version=""1.0""?>
<mavlink />")
		);

		var fileContents = additional.ToDictionary(file => file.Path, file => file.GetText()!.ToString());

		var parser = new MavlinkXmlParser();
		var builder = new MavlinkFilesTreeBuilder(parser);

		// Act
		var result = builder.Build(fileContents);

		// Assert
		Assert.Equal(2, result.Count); // Two root nodes
		var firstNode = result.SingleOrDefault(node => node.FilePath == "ThisFileShouldBeFirst.xml");
		var fourthNode = result.SingleOrDefault(node => node.FilePath == "ThisFileShouldBeFourth.xml");

		Assert.NotNull(firstNode);
		Assert.NotNull(fourthNode);
		Assert.Empty(firstNode.Includes);

		var thirdNode = fourthNode.Includes.SingleOrDefault(node => node.FilePath == "ThisFileShouldBeThird.xml");
		Assert.NotNull(thirdNode);

		var secondNode = thirdNode.Includes.SingleOrDefault(node => node.FilePath == "ThisFileShouldBeSecond.xml");
		Assert.NotNull(secondNode);
		Assert.Empty(secondNode.Includes);
	}
}
