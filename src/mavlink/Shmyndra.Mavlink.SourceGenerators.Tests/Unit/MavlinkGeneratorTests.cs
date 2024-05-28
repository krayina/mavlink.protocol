using Shmyndra.Mavlink.SourceGenerators.Protocol;

namespace Shmyndra.Mavlink.SourceGenerators.Tests.Unit;

public class MavlinkGeneratorTests
{
	[Fact]
	public Task ProtocolGenerator_GenerateAllTypes_Verify()
	{
		// arrange
		var generator = new MavlinkGenerator();

		var additional = TestsHelper.GetAdditionalTextList([
			"Stubs\\test-mavlink-common.xml",
			"Stubs\\test-mavlink-minimal.xml",
			"Stubs\\test-mavlink-standard.xml"
		]);

		// act
		var driver = generator.RunIncrementalGeneratorDriver(additional);
		var runResult = driver.GetRunResult().Results.Single();
		var generatedCode = string.Join(Environment.NewLine, runResult.GeneratedSources.Select(source => source.SourceText.ToString()));

		// assert
		return Verify(generatedCode).UseDirectory("..\\Snapshots");
	}
}
