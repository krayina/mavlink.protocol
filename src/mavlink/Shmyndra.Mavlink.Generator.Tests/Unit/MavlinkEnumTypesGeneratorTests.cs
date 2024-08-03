using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
				entries:
				[
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
				],
				deprecated: null
			)
		);
		var includes = ImmutableArray<string>.Empty;
		var filePath = "TestFile.xml";
		var namespaceName = "TestNamespace";

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEnums = enums.Select(e => generator.GenerateMavlinkEnumInternal(e, namespaceName)).ToList();
		var normalizedEnums = generatedEnums.Select(e => e.DeclarationSyntax.NormalizeWhitespace().ToFullString()).ToList();

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
				entries: [new MavlinkEnumEntry(
						name: "ESC_TYPE1",
						value: 0,
						description: "Type 1",
						details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
						deprecated: null,
						hasLocation: null,
						isDestination: null,
						missionOnly: null
					)],
				deprecated: null
			)
		);

		var enumsFile2 = ImmutableArray.Create(
			new MavlinkEnum(
				name: "ESC_CONNECTION_TYPE",
				description: "Additional types",
				bitmask: false,
				entries:
				[
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
				],
				deprecated: null
			)
		);

		var filePath1 = "File1.xml";
		var filePath2 = "File2.xml";
		var namespaceName1 = "Namespace1";
		var namespaceName2 = "Namespace2";

		var includes = ImmutableArray.Create(filePath1); // File2 includes File1

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEnumFile1 = generator.GenerateMavlinkEnumInternal(enumsFile1[0], namespaceName1);
		var generatedEnumFile2 = generator.GenerateAndMergeMavlinkEnumInternal(generatedEnumFile1, enumsFile2[0], namespaceName2);

		var allGeneratedEnums = new List<GeneratedMavlinkEnum> { generatedEnumFile1, generatedEnumFile2 };

		// Convert to normalized string representations
		var normalizedEnums = allGeneratedEnums.Select(enumDecl => enumDecl.DeclarationSyntax.NormalizeWhitespace().ToFullString()).ToList();

		// Assert with Verify
		await Verify(normalizedEnums)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters(namespaceName1, namespaceName2, filePath1, filePath2);
	}

	[Fact]
	public async Task GenerateEnumMembers_ShouldHaveObsoleteAttributeForDeprecatedEntry()
	{
		// Arrange
		ImmutableArray<MavlinkEnumEntry> deprecatedEntries = [ new MavlinkEnumEntry(
			name: "MAV_FRAME_GLOBAL_INT",
			value: 5,
			description: null,
			details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
			deprecated: new MavlinkDeprecatedInfo(
				description: "Use MAV_FRAME_GLOBAL in COMMAND_INT (and elsewhere) as a synonymous replacement.",
				since: "2024-03",
				replacedBy: "MAV_FRAME_GLOBAL",
				text: null
			),
			hasLocation: null,
			isDestination: null,
			missionOnly: null
		)];

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEntries = generator.GenerateEnumMembersInternal(deprecatedEntries, "MavFrameGlobalInt", "TestNamespace");

		// Assert
		var generatedEntry = generatedEntries.First();
		var obsoleteAttribute = generatedEntry.DeclarationSyntax.AttributeLists
			.SelectMany(al => al.Attributes)
			.FirstOrDefault(attr => attr.Name.ToString() == "System.Obsolete");

		Assert.NotNull(obsoleteAttribute);
		Assert.Contains("2024-03", obsoleteAttribute.ToFullString());
		Assert.Contains("MAV_FRAME_GLOBAL", obsoleteAttribute.ToFullString());
		Assert.Contains("Use MAV_FRAME_GLOBAL in COMMAND_INT (and elsewhere) as a synonymous replacement.", obsoleteAttribute.ToFullString());

		// Verify
		var generatedCode = generatedEntry.DeclarationSyntax.NormalizeWhitespace().ToFullString();
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("DeprecatedEntry");
	}

	[Fact]
	public async Task GenerateMavlinkEnumInternal_ShouldAddObsoleteAttribute()
	{
		// Arrange
		var deprecatedInfo = new MavlinkDeprecatedInfo(
			description: "This enum is deprecated.",
			since: "2023-01",
			replacedBy: "NewEnum",
			text: ["Additional info about deprecation."]
		);

		var mavlinkEnum = new MavlinkEnum(
			name: "OLD_ENUM",
			description: "This is an old enum.",
			bitmask: false,
			entries: ImmutableArray<MavlinkEnumEntry>.Empty,
			deprecated: deprecatedInfo
		);

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEnum = generator.GenerateMavlinkEnumInternal(mavlinkEnum, "TestNamespace");

		// Assert
		var enumDeclaration = generatedEnum.DeclarationSyntax;
		Assert.NotNull(enumDeclaration);

		var obsoleteAttribute = enumDeclaration.AttributeLists
			.SelectMany(a => a.Attributes)
			.FirstOrDefault(a => a.Name.ToString() == "System.Obsolete");

		Assert.NotNull(obsoleteAttribute);
		var argument = obsoleteAttribute?.ArgumentList?.Arguments.FirstOrDefault();
		Assert.NotNull(argument);
		Assert.Equal("\"This enum is deprecated. Since: 2023-01. Replaced by: NewEnum. Additional info about deprecation.\"", argument.ToString());

		var enumCode = generatedEnum.DeclarationSyntax.NormalizeWhitespace().ToFullString();

		await Verify(enumCode)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateAndMergeMavlinkEnumInternal_ShouldAddObsoleteAttribute_WhenEnumIsDeprecated()
	{
		// Arrange
		var existingEnum = new GeneratedMavlinkEnum(
			"Namespace1",
			"TestEnum",
			ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
			SyntaxFactory.EnumDeclaration("TestEnum"),
			new MavlinkEnum("TestEnum", "Test enum", false, ImmutableArray<MavlinkEnumEntry>.Empty, null)
		);

		var newEnumData = new MavlinkEnum(
			"TestEnum",
			"Test enum with deprecation",
			false,
			ImmutableArray<MavlinkEnumEntry>.Empty,
			new MavlinkDeprecatedInfo("This enum is deprecated", "2024-03", "NewTestEnum", null)
		);

		var generator = new MavlinkEnumGenerator();

		// Act
		var mergedEnum = generator.GenerateAndMergeMavlinkEnumInternal(existingEnum, newEnumData, "Namespace1");

		// Assert
		var obsoleteAttribute = mergedEnum.DeclarationSyntax.AttributeLists
			.SelectMany(al => al.Attributes)
			.FirstOrDefault(attr => attr.Name.ToString() == "System.Obsolete");

		Assert.NotNull(obsoleteAttribute);

		if (obsoleteAttribute?.ArgumentList?.Arguments != null)
		{
			var argument = obsoleteAttribute.ArgumentList.Arguments.First().ToString();
			Assert.Contains("This enum is deprecated", argument);
		}
		else
		{
			Assert.Fail("Obsolete attribute arguments are null or empty.");
		}

		var enumCode = mergedEnum.DeclarationSyntax.NormalizeWhitespace().ToFullString();

		await Verify(enumCode)
			.UseDirectory(SNAPSHOT_PATH);
	}
}
