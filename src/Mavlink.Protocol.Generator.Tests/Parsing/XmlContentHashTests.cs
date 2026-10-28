using AwesomeAssertions;
using Mavlink.Protocol.Generator.Parsing;

namespace Mavlink.Protocol.Generator.Tests.Parsing;

public sealed class XmlContentHashTests
{
	[Fact]
	public void Compute_IsLowercaseSha256OfUtf8()
	{
		// sha256("abc")
		XmlContentHash.Compute("abc").Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
	}

	[Fact]
	public void Compute_IgnoresLineEndingsAndBom()
	{
		var lf = XmlContentHash.Compute("<mavlink>\n</mavlink>\n");

		XmlContentHash.Compute("<mavlink>\r\n</mavlink>\r\n").Should().Be(lf);
		XmlContentHash.Compute("﻿<mavlink>\n</mavlink>\n").Should().Be(lf);
	}

	[Fact]
	public void Compute_ChangedContent_ChangesHash()
	{
		XmlContentHash.Compute("<mavlink/>").Should().NotBe(XmlContentHash.Compute("<mavlink />"));
	}
}
