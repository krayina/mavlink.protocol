using AwesomeAssertions;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Parsing;

public sealed class MavlinkXmlReaderTests
{
	private const string Path = "/dialects/test.xml";

	private static ParsedFile Read(string xml)
		=> MavlinkXmlReader.Read(Path, SourceText.From(xml), CancellationToken.None);

	[Fact]
	public void Read_HeaderElements()
	{
		var file = Read("""
			<?xml version="1.0"?>
			<mavlink>
			  <include>minimal.xml</include>
			  <include> standard.xml </include>
			  <version>3</version>
			  <dialect>0</dialect>
			  <messages/>
			</mavlink>
			""");

		file.Diagnostics.Should().BeEmpty();
		file.Path.Should().Be(Path);
		file.Includes.Select(i => i.Path).Should().Equal("minimal.xml", "standard.xml");
		file.Includes[0].Location.LineSpan.Start.Line.Should().Be(2);
		file.Version.Should().Be(3);
		file.Dialect.Should().Be(0);
		file.Hash.Should().HaveLength(64);
	}

	[Fact]
	public void Read_Enum()
	{
		var file = Read("""
			<mavlink>
			  <enums>
			    <enum name="MAV_MODE_FLAG" bitmask="true">
			      <description>Mode flags.</description>
			      <entry value="0x80" name="MAV_MODE_FLAG_SAFETY_ARMED"><description>Armed.</description></entry>
			      <entry value="64" name="MAV_MODE_FLAG_MANUAL_INPUT_ENABLED" hasLocation="true" isDestination="1"/>
			      <entry name="MAV_MODE_FLAG_AUTO"/>
			    </enum>
			    <enum name="OLD">
			      <deprecated since="2024-01" replaced_by="NEW">Use NEW.</deprecated>
			      <entry value="1" name="OLD_A"/>
			    </enum>
			  </enums>
			  <messages/>
			</mavlink>
			""");

		file.Diagnostics.Should().BeEmpty();
		var flags = file.Enums[0];
		flags.Name.Should().Be("MAV_MODE_FLAG");
		flags.IsBitmask.Should().BeTrue();
		flags.Description.Should().Be("Mode flags.");
		flags.Status.Should().BeNull();
		flags.Entries.Select(e => e.Value).Should().Equal(0x80UL, 64UL, null);
		flags.Entries[0].Description.Should().Be("Armed.");
		flags.Entries[1].Flags.Should().Be(ParsedEntryFlags.HasLocation | ParsedEntryFlags.IsDestination);

		var old = file.Enums[1];
		old.IsBitmask.Should().BeFalse();
		old.Status.Should().Be(new ParsedStatus(ParsedStatusKind.Deprecated, "2024-01", "NEW", null, "Use NEW."));
	}

	[Fact]
	public void Read_EntryParams()
	{
		var file = Read("""
			<mavlink>
			  <enums>
			    <enum name="MAV_CMD">
			      <entry value="16" name="MAV_CMD_NAV_WAYPOINT">
			        <wip/>
			        <param index="1" label="Hold" units="s" minValue="0" increment="0.5">Hold time.</param>
			        <param index="2" reserved="true" default="NaN" enum="MAV_FRAME" decimalPlaces="1" instance="true"/>
			      </entry>
			    </enum>
			  </enums>
			  <messages/>
			</mavlink>
			""");

		file.Diagnostics.Should().BeEmpty();
		var entry = file.Enums[0].Entries[0];
		entry.Status!.Kind.Should().Be(ParsedStatusKind.Wip);
		entry.Params.Should().HaveCount(2);

		var hold = entry.Params[0];
		hold.Index.Should().Be(1);
		hold.Description.Should().Be("Hold time.");
		hold.Label.Should().Be("Hold");
		hold.Units.Should().Be("s");
		hold.MinValue.Should().Be("0");
		hold.Increment.Should().Be("0.5");
		hold.Reserved.Should().BeFalse();

		var reserved = entry.Params[1];
		reserved.Reserved.Should().BeTrue();
		reserved.Default.Should().Be("NaN");
		reserved.Enum.Should().Be("MAV_FRAME");
		reserved.DecimalPlaces.Should().Be("1");
		reserved.Instance.Should().BeTrue();
		reserved.Description.Should().BeEmpty();
	}

	[Fact]
	public void Read_Message()
	{
		var file = Read("""
			<mavlink>
			  <messages>
			    <message id="253" name="STATUSTEXT">
			      <description>Status text.</description>
			      <field type="uint8_t" name="severity" enum="MAV_SEVERITY">Severity.</field>
			      <field type="char[50]" name="text">Text.</field>
			      <extensions/>
			      <field type="uint16_t" name="id" invalid="0" units="s" display="bitmask" print_format="%u" default="1" instance="true" multiplier="1E-2" increment="1" minValue="0" maxValue="9">Id.</field>
			    </message>
			  </messages>
			</mavlink>
			""");

		file.Diagnostics.Should().BeEmpty();
		var message = file.Messages.Should().ContainSingle().Subject;
		message.Id.Should().Be(253);
		message.Name.Should().Be("STATUSTEXT");
		message.Description.Should().Be("Status text.");
		message.Fields.Select(f => (f.Name, f.Type, f.IsExtension)).Should().Equal(
			("severity", "uint8_t", false),
			("text", "char[50]", false),
			("id", "uint16_t", true));
		message.Fields[0].Enum.Should().Be("MAV_SEVERITY");
		message.Fields[0].Description.Should().Be("Severity.");

		var id = message.Fields[2];
		id.Invalid.Should().Be("0");
		id.Units.Should().Be("s");
		id.Display.Should().Be("bitmask");
		id.PrintFormat.Should().Be("%u");
		id.Default.Should().Be("1");
		id.Instance.Should().BeTrue();
		id.Multiplier.Should().Be("1E-2");
		id.Increment.Should().Be("1");
		id.MinValue.Should().Be("0");
		id.MaxValue.Should().Be("9");
	}

	[Fact]
	public void Read_MalformedXml_ReportsWhereItBroke()
	{
		var file = Read("<mavlink>\n  <messages>\n</mavlink>\n");

		var diagnostic = file.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Id.Should().Be("MAVGEN001");
		diagnostic.Location!.LineSpan.Start.Line.Should().Be(2);
		file.Messages.Should().BeEmpty();
		file.Hash.Should().NotBeEmpty();
	}

	[Fact]
	public void Read_DtdIsRejected()
	{
		var file = Read("<!DOCTYPE mavlink [<!ENTITY x \"y\">]><mavlink><messages/></mavlink>");

		file.Diagnostics.Should().ContainSingle().Which.Descriptor.Should().Be(DiagnosticDescriptors.InvalidXml);
	}

	[Fact]
	public void Read_WrongRoot()
	{
		var file = Read("<dialect/>");

		MessageOf(file.Diagnostics.Should().ContainSingle().Subject).Should().Contain("<mavlink>");
	}

	[Fact]
	public void Read_MessageWithoutId_IsSkipped()
	{
		var file = Read("""
			<mavlink>
			  <messages>
			    <message name="BROKEN"><field type="uint8_t" name="a"/></message>
			    <message id="1" name="OK"><field type="uint8_t" name="a"/></message>
			  </messages>
			</mavlink>
			""");

		file.Messages.Select(m => m.Name).Should().Equal("OK");
		var diagnostic = file.Diagnostics.Should().ContainSingle().Subject;
		MessageOf(diagnostic).Should().Contain("'id'");
		diagnostic.Location!.LineSpan.Start.Line.Should().Be(2);
	}

	[Fact]
	public void Read_MessageWithBrokenField_IsSkippedAsAWhole()
	{
		var file = Read("""
			<mavlink>
			  <messages>
			    <message id="1" name="BROKEN"><field name="a"/><field type="uint8_t" name="b"/></message>
			  </messages>
			</mavlink>
			""");

		file.Messages.Should().BeEmpty();
		MessageOf(file.Diagnostics.Should().ContainSingle().Subject).Should().Contain("'type'");
	}

	[Theory]
	[InlineData("-1")]
	[InlineData("1.5")]
	[InlineData("2**3")]
	[InlineData("0x")]
	[InlineData("18446744073709551616")]
	public void Read_BadEntryValue_SkipsTheEntry(string value)
	{
		var file = Read($"""
			<mavlink>
			  <enums><enum name="E"><entry value="{value}" name="E_A"/><entry value="2" name="E_B"/></enum></enums>
			  <messages/>
			</mavlink>
			""");

		file.Enums[0].Entries.Select(e => e.Name).Should().Equal("E_B");
		file.Diagnostics.Should().ContainSingle();
	}

	[Theory]
	[InlineData("0", 0UL)]
	[InlineData(" 42 ", 42UL)]
	[InlineData("0x1F", 31UL)]
	[InlineData("0XFFFFFFFF", 4294967295UL)]
	[InlineData("18446744073709551615", ulong.MaxValue)]
	public void TryParseEntryValue_Accepts(string text, ulong expected)
	{
		MavlinkXmlReader.TryParseEntryValue(text, out var value).Should().BeTrue();
		value.Should().Be(expected);
	}

	[Fact]
	public void Read_BadBoolean_IsReportedAndTreatedAsFalse()
	{
		var file = Read("""
			<mavlink>
			  <enums><enum name="E" bitmask="yes"><entry value="1" name="E_A"/></enum></enums>
			  <messages/>
			</mavlink>
			""");

		file.Enums[0].IsBitmask.Should().BeFalse();
		MessageOf(file.Diagnostics.Should().ContainSingle().Subject).Should().Contain("bitmask");
	}

	[Fact]
	public void Read_SameContent_IsEqual()
	{
		const string xml = "<mavlink><include>a.xml</include><messages><message id=\"1\" name=\"M\"><field type=\"uint8_t\" name=\"a\"/></message></messages></mavlink>";

		Read(xml).Should().Be(Read(xml));
	}

	private static string MessageOf(DiagnosticInfo diagnostic) => diagnostic.MessageArgs[0];
}
