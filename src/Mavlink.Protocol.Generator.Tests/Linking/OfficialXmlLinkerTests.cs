using System.Collections.Immutable;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Linking;

/// <summary>
/// The official dialect XMLs linked as if one project owned them all.
/// </summary>
public sealed class OfficialXmlLinkerTests
{
	// Files starting with '_' are kept out of the repository's dialect set: their includes name
	// files without the underscore (development.xml, standard.xml), so they cannot be linked.
	private static readonly DialectGraph Graph = DialectLinker.Link(
		Directory.GetFiles(TestData.XmlDirectory, "*.xml")
			.Where(path => !Path.GetFileName(path).StartsWith('_'))
			.Select(path => MavlinkXmlReader.Read(path, SourceText.From(File.ReadAllText(path)), CancellationToken.None))
			.ToImmutableArray(),
		[],
		CancellationToken.None);

	[Fact]
	public void Link_HasNoDiagnostics()
	{
		Graph.Diagnostics.Select(d => d.ToDiagnostic().ToString()).Should().BeEmpty();
		Graph.Dialects.Should().HaveCount(11);
	}

	[Fact]
	public void Link_Ardupilotmega_SeesItsWholeTree()
	{
		Graph.Dialects.Single(d => d.Name == "ardupilotmega").Closure.Select(c => c.Name)
			.Should().Equal("common", "uAvionix", "icarous", "cubepilot", "csAirLink", "minimal");
	}

	[Fact]
	public void Link_Order_PutsIncludesFirst()
	{
		var order = Graph.Dialects.Select(d => d.Name).ToList();

		order.IndexOf("minimal").Should().BeLessThan(order.IndexOf("common"));
		order.IndexOf("common").Should().BeLessThan(order.IndexOf("ardupilotmega"));
		order.IndexOf("ardupilotmega").Should().BeLessThan(order.IndexOf("storm32"));
	}
}
