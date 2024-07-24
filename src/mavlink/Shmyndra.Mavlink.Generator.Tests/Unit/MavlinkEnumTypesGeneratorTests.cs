using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;

public class MavlinkEnumTypesGeneratorTests
{
	[Fact]
	public async Task GenerateEnums_ShouldMatchSnapshot()
	{
		// Arrange
		var enums = ImmutableArray.Create(
			new MavlinkEnum(
				name: "ESC_CONNECTION_TYPE",
				description: "Enum for ESC connection types",
				bitmask: false,
				entries: ImmutableList.Create(
					new MavlinkEnumEntry(
						name: "ESC_TYPE1",
						value: 0,
						description: "Type 1",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					),
					new MavlinkEnumEntry(
						name: "ESC_TYPE2",
						value: 1,
						description: "Type 2",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					)
				),
				deprecated: null
			),
			new MavlinkEnum(
				name: "ESC_FAILURE_FLAGS",
				description: "Bitmask for ESC failure flags",
				bitmask: true,
				entries: ImmutableList.Create(
					new MavlinkEnumEntry(
						name: "FAILURE_FLAG1",
						value: 1,
						description: "Failure Flag 1",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					),
					new MavlinkEnumEntry(
						name: "FAILURE_FLAG2",
						value: 2,
						description: "Failure Flag 2",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					)
				),
				deprecated: null
			)
		);
		var includes = ImmutableArray<string>.Empty;
		var filePath = "TestFile.xml";
		var namespaceName = "TestNamespace";

		var generator = new MavlinkEnumTypesGenerator();

		// Act
		var generatedEnums = generator.GenerateEnums(enums, namespaceName, includes, filePath, out var nameMapping);
		var normalizedEnums = generatedEnums.Select(enumDecl => enumDecl.NormalizeWhitespace().ToFullString()).ToList();

		// Assert
		await Verify(normalizedEnums)
			.UseDirectory("Snapshots/Unit/MavlinkEnumTypesGeneratorTests")
			.UseParameters(namespaceName, filePath);
	}
}
