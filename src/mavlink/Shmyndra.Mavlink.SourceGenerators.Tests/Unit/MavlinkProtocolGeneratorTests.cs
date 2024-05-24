using Microsoft.CodeAnalysis;
using Shmyndra.Mavlink.SourceGenerators.Protocol;

namespace Shmyndra.Mavlink.SourceGenerators.Tests.Unit;

public class MavlinkProtocolGeneratorTests
{
	[Fact]
	public Task ProtocolGenerator_GenerateAllTypes_Verify()
	{
		// arrange
		var controlsGenerator = new MavlinkTypesGenerator();
		var additional = TestsHelper.GetAdditionalTextList([
			"Stubs\\test-mavlink-common.xml",
			"Stubs\\test-mavlink-minimal.xml",
			"Stubs\\test-mavlink-standard.xml"
		]);
		var references = new[]
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
		};

		// act
		var driver = controlsGenerator.RunGeneratorDriver(additional, references);
		//var runResult = driver.GetRunResult().Results.Single();

		// assert
		return Verify(driver).UseDirectory("Snapshots");
	}
}
