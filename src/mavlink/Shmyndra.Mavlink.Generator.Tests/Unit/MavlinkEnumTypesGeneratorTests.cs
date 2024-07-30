using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;

public class MavlinkEnumTypesGeneratorTests
{
	private const string SNAPSHOT_PATH = "..\\Snapshots/Unit/MavlinkEnumTypesGeneratorTests";

	[Fact]
	public async Task GenerateEnums_ShouldMatchSnapshot()
	{
		// Arrange
		var enums = ImmutableArray.Create(
			new MavlinkEnum(
				name: "ESC_CONNECTION_TYPE",
				description: "Enum for ESC connection types",
				bitmask: false,
				entries:
				[
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
				],
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
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters(namespaceName, filePath);
	}

	[Fact]
	public async Task GenerateEnums_ShouldMergeEnumsFromIncludedFiles()
	{
		// Arrange
		var enumsFile1 = ImmutableArray.Create(
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
					)
				),
				deprecated: null
			)
		);

		var enumsFile2 = ImmutableArray.Create(
			new MavlinkEnum(
				name: "ESC_CONNECTION_TYPE",
				description: "Additional types",
				bitmask: false,
				entries: ImmutableList.Create(
					new MavlinkEnumEntry(
						name: "ESC_TYPE2",
						value: 1,
						description: "Type 2",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					),
					new MavlinkEnumEntry(
						name: "ESC_TYPE3",
						value: 2,
						description: "Type 3",
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

		var filePath1 = "File1.xml";
		var filePath2 = "File2.xml";
		var namespaceName1 = "Namespace1";
		var namespaceName2 = "Namespace2";

		var includes = ImmutableArray.Create(filePath1); // File2 includes File1

		var generator = new MavlinkEnumTypesGenerator();

		// Act
		var generatedEnumsFile1 = generator.GenerateEnums(enumsFile1, namespaceName1, ImmutableArray<string>.Empty, filePath1, out var nameMapping1);
		var generatedEnumsFile2 = generator.GenerateEnums(enumsFile2, namespaceName2, includes, filePath2, out var nameMapping2);

		var allGeneratedEnums = generatedEnumsFile1.Concat(generatedEnumsFile2).ToList();

		// Convert to normalized string representations
		var normalizedEnums = allGeneratedEnums.Select(enumDecl => enumDecl.NormalizeWhitespace().ToFullString()).ToList();

		// Assert with Verify
		await Verify(normalizedEnums)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters(namespaceName1, namespaceName2, filePath1, filePath2);
	}
}
