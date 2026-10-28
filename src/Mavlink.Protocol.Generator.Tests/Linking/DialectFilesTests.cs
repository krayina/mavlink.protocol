using System.Collections.Immutable;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Linking;
using Mavlink.Protocol.Generator.Tests.Harness;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mavlink.Protocol.Generator.Tests.Linking;

public sealed class DialectFilesTests
{
	[Theory]
	[InlineData("true", true)]
	[InlineData("True", true)]
	[InlineData(" true ", true)]
	[InlineData("false", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void IsOwned_ReadsTheMetadata(string? value, bool expected)
	{
		var file = new TestAdditionalText("/x/common.xml", "<mavlink/>");
		var metadata = value is null
			? ImmutableDictionary<string, string>.Empty
			: ImmutableDictionary<string, string>.Empty.Add(GeneratorInput.ItemMetadataPrefix + DialectFiles.MetadataName, value);
		var options = new TestAnalyzerConfigOptionsProvider(
			ImmutableDictionary<string, string>.Empty,
			ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty.Add(file.Path, metadata));

		DialectFiles.IsOwned(file, options).Should().Be(expected);
	}

	[Fact]
	public void Read_UnreadableFile_IsReported()
	{
		var file = DialectFiles.Read(new UnreadableText("/x/common.xml"), CancellationToken.None);

		file.Diagnostics.Should().ContainSingle().Which.Descriptor.Id.Should().Be("MAVGEN001");
		file.Messages.Should().BeEmpty();
	}

	private sealed class UnreadableText(string path) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText? GetText(CancellationToken cancellationToken = default) => null;
	}
}
