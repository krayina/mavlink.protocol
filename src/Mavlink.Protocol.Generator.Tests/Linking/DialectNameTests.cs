using AwesomeAssertions;
using Mavlink.Protocol.Generator.Linking;

namespace Mavlink.Protocol.Generator.Tests.Linking;

public sealed class DialectNameTests
{
	[Theory]
	[InlineData("common.xml", "common")]
	[InlineData("../xml/common.xml", "common")]
	[InlineData("/home/user/xml/common.xml", "common")]
	[InlineData(@"C:\xml\Common.XML", "Common")]
	[InlineData(@"sub\csAirLink.xml", "csAirLink")]
	[InlineData("common", "common")]
	[InlineData("common.v2.xml", "common.v2")]
	public void FromPath(string path, string expected)
	{
		DialectName.FromPath(path).Should().Be(expected);
	}

	[Fact]
	public void Comparer_IgnoresCase()
	{
		DialectName.Comparer.Equals("csAirLink", "csairlink").Should().BeTrue();
	}
}
