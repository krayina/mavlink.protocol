using AwesomeAssertions;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Parsing;

/// <summary>
/// The reader against every official dialect XML, checked with pymavlink's view of the same files.
/// </summary>
public sealed class OfficialXmlReaderTests
{
	[Theory]
	[MemberData(nameof(TestData.OfficialXmlNames), MemberType = typeof(TestData))]
	public void Read_HasNoDiagnostics(string fileName)
	{
		var file = Read(fileName);

		file.Diagnostics.Select(d => d.MessageArgs[0]).Should().BeEmpty();
	}

	[Theory]
	[MemberData(nameof(TestData.GoldenXmlNames), MemberType = typeof(TestData))]
	public void Read_MessagesMatchPymavlink(string fileName)
	{
		var file = Read(fileName);
		var golden = TestData.Golden(fileName);

		file.Messages.Select(m => (m.Id, m.Name)).Should().BeEquivalentTo(golden.Messages.Select(m => (m.Id, m.Name)));

		foreach (var expected in golden.Messages)
		{
			var message = file.Messages.Single(m => m.Id == expected.Id);

			// Golden fields are in wire order; the reader keeps XML order, so compare as sets.
			message.Fields.Select(f => (f.Name, f.IsExtension)).Should().BeEquivalentTo(
				expected.Fields.Select(f => (f.Name, f.IsExtension)),
				because: $"{expected.Name} has these fields");
		}
	}

	private static ParsedFile Read(string fileName)
	{
		var path = TestData.XmlPath(fileName);
		return MavlinkXmlReader.Read(path, SourceText.From(File.ReadAllText(path)), CancellationToken.None);
	}
}
