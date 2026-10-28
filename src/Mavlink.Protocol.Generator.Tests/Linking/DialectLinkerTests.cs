using System.Collections.Immutable;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Linking;

public sealed class DialectLinkerTests
{
	[Fact]
	public void Link_Diamond_OrdersIncludesFirstAndKeepsEachDialectOnce()
	{
		var graph = Link(
			Xml("/x/common.xml", "minimal.xml", "standard.xml"),
			Xml("/x/standard.xml", "minimal.xml"),
			Xml("/x/minimal.xml"));

		graph.Diagnostics.Should().BeEmpty();
		graph.Dialects.Select(d => d.Name).Should().Equal("minimal", "standard", "common");

		var common = graph.Dialects.Single(d => d.Name == "common");
		common.Includes.Should().Equal(Own("minimal"), Own("standard"));
		common.Closure.Should().Equal(Own("minimal"), Own("standard"));
		graph.Dialects.Single(d => d.Name == "minimal").Closure.Should().BeEmpty();
	}

	[Fact]
	public void Link_Closure_IsNearestFirst()
	{
		var graph = Link(
			Xml("/x/ardupilotmega.xml", "common.xml", "cubepilot.xml"),
			Xml("/x/cubepilot.xml", "common.xml"),
			Xml("/x/common.xml", "minimal.xml"),
			Xml("/x/minimal.xml"));

		graph.Dialects.Single(d => d.Name == "ardupilotmega").Closure
			.Should().Equal(Own("common"), Own("cubepilot"), Own("minimal"));
	}

	[Fact]
	public void Link_IncludeIgnoresFoldersAndCase()
	{
		var graph = Link(
			Xml("/project/custom.xml", "../shared/CSAIRLINK.xml"),
			Xml("/shared/csAirLink.xml"));

		graph.Diagnostics.Should().BeEmpty();
		graph.Dialects.Single(d => d.Name == "custom").Includes.Should().Equal(Own("csAirLink"));
	}

	[Fact]
	public void Link_RepeatedInclude_IsKeptOnce()
	{
		var graph = Link(
			Xml("/x/common.xml", "minimal.xml", "minimal.xml"),
			Xml("/x/minimal.xml"));

		graph.Diagnostics.Should().BeEmpty();
		graph.Dialects.Single(d => d.Name == "common").Includes.Should().Equal(Own("minimal"));
	}

	[Fact]
	public void Link_UnknownInclude_IsReportedAtTheInclude()
	{
		var graph = Link(Xml("/x/custom.xml", "minimal.xml", "common.xml"), Xml("/x/minimal.xml"));

		var diagnostic = graph.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.UnresolvedInclude);
		diagnostic.MessageArgs.Should().Equal("custom.xml", "common.xml");
		diagnostic.Location!.FilePath.Should().Be("/x/custom.xml");
		diagnostic.Location.LineSpan.Start.Line.Should().Be(3, "common.xml is the second include");

		graph.Dialects.Single(d => d.Name == "custom").Includes.Should().Equal(Own("minimal"));
	}

	[Fact]
	public void Link_ReferencedDialect_ResolvesAndFollowsItsIncludes()
	{
		var graph = DialectLinker.Link(
			ImmutableArray.Create(Xml("/x/custom.xml", "common.xml")),
			ImmutableArray.Create(
				new ReferencedDialect("common", ImmutableArray.Create("minimal").ToEquatableArray()),
				new ReferencedDialect("minimal", EquatableArray<string>.Empty)),
			CancellationToken.None);

		graph.Diagnostics.Should().BeEmpty();
		var custom = graph.Dialects.Should().ContainSingle().Subject;
		custom.Includes.Should().Equal(Referenced("common"));
		custom.Closure.Should().Equal(Referenced("common"), Referenced("minimal"));
	}

	[Fact]
	public void Link_Cycle_IsReportedOnceAndBroken()
	{
		var graph = Link(
			Xml("/x/a.xml", "b.xml"),
			Xml("/x/b.xml", "c.xml"),
			Xml("/x/c.xml", "a.xml"));

		var diagnostic = graph.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.IncludeCycle);
		diagnostic.MessageArgs.Should().Equal("a.xml -> b.xml -> c.xml -> a.xml");
		diagnostic.Location!.FilePath.Should().Be("/x/c.xml");

		graph.Dialects.Select(d => d.Name).Should().Equal("c", "b", "a");
		graph.Dialects.Single(d => d.Name == "c").Includes.Should().BeEmpty();
	}

	[Fact]
	public void Link_SelfInclude_IsACycle()
	{
		var graph = Link(Xml("/x/a.xml", "a.xml"));

		graph.Diagnostics.Should().ContainSingle().Which.MessageArgs.Should().Equal("a.xml -> a.xml");
		graph.Dialects.Should().ContainSingle().Which.Includes.Should().BeEmpty();
	}

	[Fact]
	public void Link_TwoFilesWithTheSameName_KeepsTheFirstByPath()
	{
		var graph = Link(Xml("/y/Common.xml"), Xml("/x/common.xml"));

		var diagnostic = graph.Diagnostics.Should().ContainSingle().Subject;
		diagnostic.Descriptor.Should().Be(DiagnosticDescriptors.DuplicateDialectFile);
		diagnostic.Location!.FilePath.Should().Be("/y/Common.xml");
		diagnostic.MessageArgs.Should().Equal("common", "/x/common.xml");

		graph.Dialects.Should().ContainSingle().Which.File.Path.Should().Be("/x/common.xml");
	}

	[Fact]
	public void Link_DoesNotDependOnInputOrder()
	{
		var files = new[]
		{
			Xml("/x/common.xml", "minimal.xml", "standard.xml"),
			Xml("/x/standard.xml", "minimal.xml"),
			Xml("/x/minimal.xml"),
			Xml("/x/a.xml", "b.xml", "missing.xml"),
			Xml("/x/b.xml", "a.xml"),
		};

		Link(files).Should().Be(Link(files.Reverse().ToArray()));
	}

	private static DialectGraph Link(params ParsedFile[] files)
		=> DialectLinker.Link(files.ToImmutableArray(), ImmutableArray<ReferencedDialect>.Empty, CancellationToken.None);

	private static ParsedFile Xml(string path, params string[] includes)
	{
		// The first include is on line index 2, the next ones follow; location tests rely on it.
		var xml = "<?xml version=\"1.0\"?>\n<mavlink>\n"
			+ string.Concat(includes.Select(i => $"  <include>{i}</include>\n"))
			+ "  <messages/>\n</mavlink>\n";
		return MavlinkXmlReader.Read(path, SourceText.From(xml), CancellationToken.None);
	}

	private static DialectReference Own(string name) => new(name, DialectOrigin.Own);

	private static DialectReference Referenced(string name) => new(name, DialectOrigin.Referenced);
}
