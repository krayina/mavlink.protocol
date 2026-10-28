using AwesomeAssertions;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Parsing;

public sealed class FieldTypeTests
{
	[Theory]
	[InlineData("char", "char", 0, 1)]
	[InlineData("int8_t", "int8_t", 0, 1)]
	[InlineData("uint8_t", "uint8_t", 0, 1)]
	[InlineData("int16_t", "int16_t", 0, 2)]
	[InlineData("uint16_t", "uint16_t", 0, 2)]
	[InlineData("int32_t", "int32_t", 0, 4)]
	[InlineData("uint32_t", "uint32_t", 0, 4)]
	[InlineData("int64_t", "int64_t", 0, 8)]
	[InlineData("uint64_t", "uint64_t", 0, 8)]
	[InlineData("float", "float", 0, 4)]
	[InlineData("double", "double", 0, 8)]
	[InlineData("char[50]", "char", 50, 50)]
	[InlineData("float[4]", "float", 4, 16)]
	[InlineData("uint8_t[255]", "uint8_t", 255, 255)]
	[InlineData("uint16_t[1]", "uint16_t", 1, 2)]
	// The element is passed by its schema name: WireType is internal and cannot be a public test parameter.
	public void TryParse_Valid(string text, string element, int arrayLength, int wireLength)
	{
		FieldType.TryParse(text, out var type, out var error).Should().BeTrue();

		error.Should().BeNull();
		type.ElementName.Should().Be(element);
		type.ArrayLength.Should().Be(arrayLength);
		type.WireLength.Should().Be(wireLength);
		type.IsMavlinkVersion.Should().BeFalse();
		type.ToString().Should().Be(text);
	}

	[Fact]
	public void TryParse_MavlinkVersion_IsUInt8()
	{
		FieldType.TryParse("uint8_t_mavlink_version", out var type, out _).Should().BeTrue();

		type.Should().Be(new FieldType(WireType.UInt8, 0, IsMavlinkVersion: true));
		type.ElementName.Should().Be("uint8_t");
		type.WireLength.Should().Be(1);
		type.ToString().Should().Be("uint8_t_mavlink_version");
	}

	[Theory]
	[InlineData("uint8")]                       // pymavlink accepts it; the schema does not
	[InlineData("array[4]")]                    // the same
	[InlineData("UINT8_T")]
	[InlineData("uint8_t ")]
	[InlineData("bool")]
	[InlineData("uint8_t[0]")]
	[InlineData("uint8_t[256]")]
	[InlineData("uint8_t[-1]")]
	[InlineData("uint8_t[]")]
	[InlineData("uint8_t[4")]
	[InlineData("uint8_t[4][2]")]
	[InlineData("uint8_t_mavlink_version[2]")]
	[InlineData("")]
	public void TryParse_Invalid(string text)
	{
		FieldType.TryParse(text, out _, out var error).Should().BeFalse();

		error.Should().NotBeNullOrEmpty();
	}

	[Theory]
	[MemberData(nameof(TestData.GoldenXmlNames), MemberType = typeof(TestData))]
	public void TryParse_OfficialFields_MatchPymavlink(string fileName)
	{
		var path = TestData.XmlPath(fileName);
		var file = MavlinkXmlReader.Read(path, SourceText.From(File.ReadAllText(path)), CancellationToken.None);

		foreach (var expected in TestData.Golden(fileName).Messages)
		{
			foreach (var field in file.Messages.Single(m => m.Id == expected.Id).Fields)
			{
				FieldType.TryParse(field.Type, out var type, out var error).Should().BeTrue(error);

				var golden = expected.Fields.Single(f => f.Name == field.Name);
				(type.ElementName, type.ArrayLength).Should().Be((golden.Type, golden.ArrayLength), $"{expected.Name}.{field.Name}");
			}
		}
	}
}
