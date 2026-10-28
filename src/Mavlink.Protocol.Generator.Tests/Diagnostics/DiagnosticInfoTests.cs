using System.Globalization;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Diagnostics;

public sealed class DiagnosticInfoTests
{
#pragma warning disable RS2008 // A test-only rule, not a shipped analyzer rule.
	private static readonly DiagnosticDescriptor TestRule = new(
		"TEST001", "Test", "Value '{0}' in {1}", "Test", DiagnosticSeverity.Error, isEnabledByDefault: true);
#pragma warning restore RS2008

	private static readonly SourceText Xml = SourceText.From("<mavlink>\n  <include>common.xml</include>\n</mavlink>\n");

	[Fact]
	public void Equals_SameContent_IsTrue()
	{
		var left = DiagnosticInfo.Create(TestRule, LocationInfo.FromLineColumn("/a.xml", Xml, 2, 4, 7), "x", "y");
		var right = DiagnosticInfo.Create(TestRule, LocationInfo.FromLineColumn("/a.xml", Xml, 2, 4, 7), "x", "y");

		right.Should().Be(left);
		right.GetHashCode().Should().Be(left.GetHashCode());
	}

	[Fact]
	public void ToDiagnostic_KeepsMessageAndLocation()
	{
		// IXmlLineInfo of <include> reports line 2, column 4 (the element name).
		var info = DiagnosticInfo.Create(TestRule, LocationInfo.FromLineColumn("/a.xml", Xml, 2, 4, 7), "x", "y");

		var diagnostic = info.ToDiagnostic();

		diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Be("Value 'x' in y");
		var span = diagnostic.Location.GetLineSpan();
		span.Path.Should().Be("/a.xml");
		span.StartLinePosition.Should().Be(new LinePosition(1, 3));
		span.EndLinePosition.Should().Be(new LinePosition(1, 10));
		Xml.ToString(diagnostic.Location.SourceSpan).Should().Be("include");
	}

	[Fact]
	public void FromLineColumn_OutOfRange_ClampsToTheText()
	{
		var pastLine = LocationInfo.FromLineColumn("/a.xml", Xml, 1, 5, 100);
		var noLine = LocationInfo.FromLineColumn("/a.xml", Xml, 99, 1, 1);

		Xml.ToString(pastLine.Span).Should().Be("link>");
		noLine.Span.Should().Be(default(TextSpan));
	}

	[Fact]
	public void ToDiagnostic_WithoutLocation_UsesNone()
	{
		var diagnostic = DiagnosticInfo.Create(TestRule, null, "x", "y").ToDiagnostic();

		diagnostic.Location.Should().Be(Location.None);
	}
}
