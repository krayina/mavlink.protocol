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
		// Arrange:
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
		var namespaceName = "TestNamespace";

		var generator = new MavlinkEnumGenerator();

		// Act:
		var generatedEnums = enums.Select(e => generator.GenerateMavlinkEnum(e, namespaceName)).ToList();
		var normalizedEnums = generatedEnums.Select(e => e.DeclarationSyntax.ToNormalizedString()).ToList();

		// Assert:
		await Verify(normalizedEnums)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters(namespaceName);
	}

	[Fact]
	public async Task GenerateEnums_ShouldMergeEnumsFromIncludedNamespaces()
	{
		// Arrange:
		var enum1 = new MavlinkEnum(
			name: "ESC_CONNECTION_TYPE",
			description: "Enum for ESC connection types",
			bitmask: false,
			entries: [new MavlinkEnumEntry("ESC_TYPE1", 0, "Type 1", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)],
			deprecated: null
		);

		var enum2 = new MavlinkEnum(
			name: "ESC_CONNECTION_TYPE",
			description: "Additional types",
			bitmask: false,
			entries: [
				new MavlinkEnumEntry("ESC_TYPE2", 1, "Type 2", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				new MavlinkEnumEntry("ESC_TYPE3", 2, "Type 3", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
			],
			deprecated: null
		);

		var node1 = new MavlinkNode(
			"Namespace1",
			new MavlinkData(
				enums: [enum1],
				messages: ImmutableArray<MavlinkMessage>.Empty,
				includes: ImmutableArray<string>.Empty,
				version: null,
				dialect: null
			),
			new List<MavlinkNode>()
		);
		var node2 = new MavlinkNode(
			"Namespace2",
			new MavlinkData(
				enums: [enum2],
				messages: ImmutableArray<MavlinkMessage>.Empty,
				includes: ["Namespace1"],
				version: null,
				dialect: null
			),
			new List<MavlinkNode> { node1 }
		);

		var enumGenerator = new MavlinkEnumGenerator();
		var enumTreeGenerator = new MavlinkEnumTreeGenerator(enumGenerator);

		// Act:
		var generatedEnum1 = enumGenerator.GenerateMavlinkEnum(enum1, "Namespace1");
		var generatedEnum2 = enumTreeGenerator.GenerateEnums(node2, "Namespace2");

		// Assert:
		var normalizedEnums = generatedEnum2.Insert(0, generatedEnum1).Select(e => e.DeclarationSyntax.ToNormalizedString()).ToList();
		await Verify(normalizedEnums)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Namespace1", "Namespace2");
	}

	[Fact]
	public async Task GenerateEnum_ShouldHaveObsoleteAttributeForDeprecatedEntry()
	{
		// Arrange
		ImmutableArray<MavlinkEnumEntry> deprecatedEntries = [
			new MavlinkEnumEntry(
			name: "MAV_FRAME_GLOBAL_INT_Property",
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
		)
		];

		var mavlinkEnum = new MavlinkEnum(
			name: "MavFrameGlobalInt",
			description: null,
			bitmask: null,
			entries: deprecatedEntries,
			deprecated: null
		);

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEnum = generator.GenerateMavlinkEnum(mavlinkEnum, "TestNamespace");
		var generatedEntry = generatedEnum.GeneratedEntries.First();
		var a = generatedEntry.DeclarationSyntax.ToNormalizedString();
		// Assert
		var obsoleteAttribute = generatedEntry.DeclarationSyntax.AttributeLists
			.SelectMany(al => al.Attributes)
			.FirstOrDefault(attr => attr.Name.ToString() == "Obsolete");

		Assert.NotNull(obsoleteAttribute);
		Assert.Contains("2024-03", obsoleteAttribute.ToFullString());
		Assert.Contains("MAV_FRAME_GLOBAL", obsoleteAttribute.ToFullString());
		Assert.Contains("Use MAV_FRAME_GLOBAL in COMMAND_INT (and elsewhere) as a synonymous replacement.", obsoleteAttribute.ToFullString());

		// Verify
		var generatedCode = generatedEnum.DeclarationSyntax.ToNormalizedString();
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("DeprecatedEntry");
	}

	[Fact]
	public async Task GenerateMavlinkEnum_ShouldAddObsoleteAttribute()
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
		var generatedEnum = generator.GenerateMavlinkEnum(mavlinkEnum, "TestNamespace");

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

		var enumCode = generatedEnum.DeclarationSyntax.ToNormalizedString();

		await Verify(enumCode)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateAndMergeMavlinkEnum_ShouldAddObsoleteAttribute_WhenEnumIsDeprecated()
	{
		// Arrange
		var existingEnum = new GeneratedMavlinkEnum(
			"Namespace1",
			"TestEnum",
			null,
			ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
			SyntaxFactory.EnumDeclaration("TestEnum"),
			new MavlinkEnum("TestEnum", "Test enum", false, ImmutableArray<MavlinkEnumEntry>.Empty, null)
		);

		var newEnumData = new MavlinkEnum(
			name: "TestEnum",
			description: "Test enum with deprecation",
			bitmask: false,
			entries: ImmutableArray<MavlinkEnumEntry>.Empty,
			deprecated: new MavlinkDeprecatedInfo("This enum is deprecated", "2024-03", "NewTestEnum", null)
		);

		var generator = new MavlinkEnumGenerator();

		// Act
		var mergedEnum = generator.GenerateAndMergeMavlinkEnum(
			newEnumData,
			"Namespace1",
			[existingEnum]
		);

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

		var enumCode = mergedEnum.DeclarationSyntax.ToNormalizedString();

		await Verify(enumCode)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateEnum_ShouldIncludeDocumentationForEnumAndMembers()
	{
		// Arrange
		ImmutableArray<MavlinkEnumEntry> entries = [
			new MavlinkEnumEntry(
			name: "MAV_FRAME_GLOBAL_INT_Property",
			value: 5,
			description: "Global integer frame description",
			details: ImmutableArray<MavlinkEnumEntryDetail>.Empty,
			deprecated: null,
			hasLocation: null,
			isDestination: null,
			missionOnly: null
		)
		];

		var mavlinkEnum = new MavlinkEnum(
			name: "MavFrameGlobalInt",
			description: "Mavlink frame enumeration",
			bitmask: false,
			entries: entries,
			deprecated: null
		);

		var generator = new MavlinkEnumGenerator();

		// Act
		var generatedEnum = generator.GenerateMavlinkEnum(mavlinkEnum, "TestNamespace");
		var generatedEntry = generatedEnum.GeneratedEntries.First();

		// Assert
		var generatedCode = generatedEnum.DeclarationSyntax.ToFullString();
		Assert.Contains("/// <summary>", generatedCode);
		Assert.Contains("/// Mavlink frame enumeration", generatedCode);
		Assert.Contains("/// <remarks>", generatedCode);
		Assert.Contains("/// Original name: MavFrameGlobalInt", generatedCode);

		Assert.Contains("/// <summary>", generatedEntry.DeclarationSyntax.ToFullString());
		Assert.Contains("/// Global integer frame description", generatedCode);
		Assert.Contains("/// <remarks>", generatedCode);
		Assert.Contains("/// Original name: MAV_FRAME_GLOBAL_INT", generatedCode);

		var enumSummary = generatedEnum.DeclarationSyntax.GetLeadingTrivia()
			.Select(t => t.ToString())
			.FirstOrDefault(s => s.Contains("<summary>"));
		var enumRemarks = generatedEnum.DeclarationSyntax.GetLeadingTrivia()
			.Select(t => t.ToString())
			.FirstOrDefault(s => s.Contains("<remarks>"));
		Assert.NotNull(enumSummary);
		Assert.Contains("Mavlink frame enumeration", enumSummary);
		Assert.NotNull(enumRemarks);
		Assert.Contains("Original name: MavFrameGlobalInt", enumRemarks);

		var entrySummary = generatedEntry.DeclarationSyntax.GetLeadingTrivia()
			.Select(t => t.ToString())
			.FirstOrDefault(s => s.Contains("<summary>"));
		var entryRemarks = generatedEntry.DeclarationSyntax.GetLeadingTrivia()
			.Select(t => t.ToString())
			.FirstOrDefault(s => s.Contains("<remarks>"));
		Assert.NotNull(entrySummary);
		Assert.NotNull(entryRemarks);

		// Verify
		var normalizedCode = generatedEnum.DeclarationSyntax.ToNormalizedString();
		await Verify(normalizedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Documentation");
	}

	[Fact]
	public async Task GenerateAndMergeMavlinkEnum_ShouldMergeThreeEnumsCorrectly()
	{
		// Arrange
		var generator = new MavlinkEnumGenerator();

		var enum1 = new MavlinkEnum(
			name: "Test_Enum",
			entries: [
				new MavlinkEnumEntry("Value_A", 1, "First value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				new MavlinkEnumEntry("Value_B", 2, "Second value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
			],
			bitmask: false,
			description: "First enum",
			deprecated: null
		);

		var enum2 = new MavlinkEnum(
			name: "Test_Enum",
			entries: [
				new MavlinkEnumEntry("Value_C", 3, "Third value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				new MavlinkEnumEntry("Value_D", 4, "Fourth value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
			],
			bitmask: false,
			description: "Second enum",
			deprecated: null
		);

		var enum3 = new MavlinkEnum(
			name: "Test_Enum",
			entries: [
				new MavlinkEnumEntry("Value_E", 5, "Fifth value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				new MavlinkEnumEntry("Value_F", 6, "Sixth value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
			],
			bitmask: false,
			description: "Third enum",
			deprecated: null
		);

		// Act
		var generatedEnum1 = generator.GenerateMavlinkEnum(enum1, "Namespace1");
		var mergedEnum1 = generator.GenerateAndMergeMavlinkEnum(enum2, "Namespace2", [generatedEnum1]);
		var mergedEnum2 = generator.GenerateAndMergeMavlinkEnum(enum3, "Namespace3", [mergedEnum1]);
		var result = mergedEnum2.DeclarationSyntax.ToNormalizedString();

		// Assert
		Assert.Equal("TestEnum", mergedEnum2.GeneratedName);
		Assert.Equal("Namespace3", mergedEnum2.Namespace);

		var expectedEntries = new List<string> { "ValueA", "ValueB", "ValueC", "ValueD", "ValueE", "ValueF" };
		var actualEntries = mergedEnum2.GeneratedEntries.Select(e => e.GeneratedName).ToList();

		Assert.Equal(expectedEntries.Count, actualEntries.Count);
		foreach (var entry in expectedEntries)
		{
			Assert.Contains(entry, actualEntries);
		}

		await Verify(result)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateMavlinkEnumSpecificBitmask_WithUShortUnderlying__ShouldGenerateEnumBitmaskStruct()
	{
		// Arrange
		var enumBitmaskGenerator = new MavlinkSpecificEnumBitmaskGenerator();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var enum1 = new GeneratedMavlinkEnum(
			"Namespace1",
			"TestEnum",
			null,
			new List<GeneratedMavlinkEnumEntry>()
			{
				new GeneratedMavlinkEnumEntry("Namespace1", "ValueA", null, new MavlinkEnumEntry("Value_A", 1, "First value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
				new GeneratedMavlinkEnumEntry("Namespace1", "ValueB", null, new MavlinkEnumEntry("Value_B", 2, "Second value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null))
			}.ToImmutableArray(),
			null,
			new MavlinkEnum(
				name: "Test_Enum",
				entries:
				[
					new MavlinkEnumEntry("Value_A", 1, "First value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
					new MavlinkEnumEntry("Value_B", 2, "Second value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				],
				bitmask: true,
				description: "First enum",
				deprecated: null
			)
		);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Act:
		var generatedEnumBitmaskType = enumBitmaskGenerator.Generate(enum1, "ushort").ToNormalizedString();

		// Assert:
		await Verify(generatedEnumBitmaskType)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateMavlinkEnumGenericBitmask_WithByteUnderlying__ShouldGenerateEnumBitmaskStruct()
	{
		// Arrange
		var enumBitmaskGenerator = new MavlinkGenericEnumBitmaskGenerator();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var enum1 = new GeneratedMavlinkEnum(
			"Namespace1",
			"TestEnum",
			null,
			new List<GeneratedMavlinkEnumEntry>()
			{
				new GeneratedMavlinkEnumEntry("Namespace1", "ValueA", null, new MavlinkEnumEntry("Value_A", 1, "First value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
				new GeneratedMavlinkEnumEntry("Namespace1", "ValueB", null, new MavlinkEnumEntry("Value_B", 2, "Second value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null))
			}.ToImmutableArray(),
			null,
			new MavlinkEnum(
				name: "Test_Enum",
				entries:
				[
					new MavlinkEnumEntry("Value_A", 1, "First value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
					new MavlinkEnumEntry("Value_B", 2, "Second value", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
				],
				bitmask: true,
				description: "First enum",
				deprecated: null
			)
		);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Act:
		var generatedEnumBitmaskType = enumBitmaskGenerator.Generate(enum1);

		// Assert:
		await Verify(generatedEnumBitmaskType)
			.UseDirectory(SNAPSHOT_PATH);
	}

	[Fact]
	public async Task GenerateMavlinkEscFailureFlagsBitmask_WithByteBaseType__ShouldGenerateEscFailureFlagsEnumBitmask()
	{
		// Arrange:
		var enumBitmaskGenerator = new MavlinkGenericEnumBitmaskGenerator();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var enum1 = new GeneratedMavlinkEnum(
			"Namespace1",
			"EscFailureFlags",
			"byte",
			new List<GeneratedMavlinkEnumEntry>
			{
					new GeneratedMavlinkEnumEntry("Namespace1", "None", null, new MavlinkEnumEntry("None", 0, "No failure", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "OverCurrent", null, new MavlinkEnumEntry("OverCurrent", 1, "Over current", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "OverVoltage", null, new MavlinkEnumEntry("OverVoltage", 2, "Over voltage", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "OverTemperature", null, new MavlinkEnumEntry("OverTemperature", 4, "Over temperature", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "OverRpm", null, new MavlinkEnumEntry("OverRpm", 8, "Over RPM", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "InconsistentCmd", null, new MavlinkEnumEntry("InconsistentCmd", 16, "Inconsistent command", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "MotorStuck", null, new MavlinkEnumEntry("MotorStuck", 32, "Motor stuck", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)),
					new GeneratedMavlinkEnumEntry("Namespace1", "Generic", null, new MavlinkEnumEntry("Generic", 64, "Generic failure", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null))
			}.ToImmutableArray(),
			null,
			new MavlinkEnum(
				name: "EscFailureFlags",
				entries:
				[
						new MavlinkEnumEntry("None", 0, "No failure", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("OverCurrent", 1, "Over current", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("OverVoltage", 2, "Over voltage", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("OverTemperature", 4, "Over temperature", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("OverRpm", 8, "Over RPM", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("InconsistentCmd", 16, "Inconsistent command", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("MotorStuck", 32, "Motor stuck", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
						new MavlinkEnumEntry("Generic", 64, "Generic failure", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
				],
				bitmask: true,
				description: "ESC failure flags",
				deprecated: null
			)
		);
#pragma warning restore IDE0305 // Simplify collection initialization

		// Act:
		var generatedEnumBitmaskType = enumBitmaskGenerator.Generate(enum1);

		// Assert:
		await Verify(generatedEnumBitmaskType)
			.UseDirectory(SNAPSHOT_PATH);
	}
}
