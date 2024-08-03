using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;

public class MavlinkMessagesGeneratorTests
{
	private const string SNAPSHOT_PATH = "..\\Snapshots/Unit/MavlinkMessageTypesGeneratorTests";

	private static readonly ImmutableArray<MavlinkEnum> MavlinkEnums =
	[
		new MavlinkEnum("ESC_CONNECTION_TYPE", "Indicates the ESC connection type.", false,
		[
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_PPM", 0, "Traditional PPM ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_SERIAL", 1, "Serial Bus connected ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_ONESHOT", 2, "One Shot PPM ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_I2C", 3, "I2C ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_CAN", 4, "CAN-Bus ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_CONNECTION_TYPE_DSHOT", 5, "DShot ESC.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
		], null),
		new MavlinkEnum("ESC_FAILURE_FLAGS", "Flags to report ESC failures.", true,
		[
			new MavlinkEnumEntry("ESC_FAILURE_NONE", 0, "No ESC failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_OVER_CURRENT", 1, "Over current failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_OVER_VOLTAGE", 2, "Over voltage failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_OVER_TEMPERATURE", 4, "Over temperature failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_OVER_RPM", 8, "Over RPM failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_INCONSISTENT_CMD", 16, "Inconsistent command failure i.e. out of bounds.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_MOTOR_STUCK", 32, "Motor stuck failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null),
			new MavlinkEnumEntry("ESC_FAILURE_GENERIC", 64, "Generic ESC failure.", ImmutableArray<MavlinkEnumEntryDetail>.Empty, null, null, null, null)
		], null)
	];

	private static readonly ImmutableArray<GeneratedMavlinkEnum> GeneratedEnums =
	[
		new GeneratedMavlinkEnum("Namespace1", "EscConnectionType", ImmutableArray<GeneratedMavlinkEnumEntry>.Empty, SyntaxFactory.EnumDeclaration("EscConnectionType"), MavlinkEnums[0]),
		new GeneratedMavlinkEnum("Namespace1", "EscFailureFlags", ImmutableArray<GeneratedMavlinkEnumEntry>.Empty, SyntaxFactory.EnumDeclaration("EscFailureFlags"), MavlinkEnums[1])
	];

	private static readonly ImmutableArray<MavlinkMessageField> MavlinkFields =
	[
		new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "index", "Index of the first ESC in this message", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, 4, 0, 60, true, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("uint64_t"), "time_usec", "Timestamp", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Us, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("uint16_t"), "counter", "Counter of data packets received", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "count", "Total number of ESCs in all messages of this type", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldEnumType("uint8_t", "ESC_CONNECTION_TYPE"), "connection_type", "Connection type protocol for all ESC", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "info", "Information regarding online/offline status of each ESC", MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldEnumType("uint16_t[4]", "ESC_FAILURE_FLAGS"), "failure_flags", "Bitmap of ESC failure flags", MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("uint32_t[4]"), "error_count", "Number of reported errors by each ESC since boot", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new MavlinkMessageField(new MavlinkMessageFieldType("int16_t[4]"), "temperature", "Temperature of each ESC", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.CdegC, true, null, null, null, null, null, null, "[INT16_MAX]")
	];

	private static readonly ImmutableArray<GeneratedMavlinkMessageField> GeneratedFields =
	[
		new GeneratedMavlinkMessageField("Index", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[0]),
		new GeneratedMavlinkMessageField("TimeUsec", new GeneratedMavlinkMessageFieldType("uint64_t", "ulong"),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[1]),
		new GeneratedMavlinkMessageField("Counter", new GeneratedMavlinkMessageFieldType("uint16_t", "ushort"),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[2]),
		new GeneratedMavlinkMessageField("Count", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[3]),
		new GeneratedMavlinkMessageField("ConnectionType", new GeneratedMavlinkMessageFieldEnumType("uint8_t", "byte", GeneratedEnums[0]),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[4]),
		new GeneratedMavlinkMessageField("Info", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[5]),
		new GeneratedMavlinkMessageField("FailureFlags", new GeneratedMavlinkMessageFieldArrayEnumType("uint16_t[4]", "ushort", GeneratedEnums[1], 4),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[6]),
		new GeneratedMavlinkMessageField("ErrorCount", new GeneratedMavlinkMessageFieldArrayType("uint32_t[4]", "uint", 4),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[7]),
		new GeneratedMavlinkMessageField("Temperature", new GeneratedMavlinkMessageFieldArrayType("int16_t[4]", "short", 4),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[8])
	];

	private static readonly ImmutableArray<MavlinkMessage> MavlinkMessages =
	[
		new MavlinkMessage(290, "ESC_INFO", "ESC information for lower rate streaming. Recommended streaming rate 1Hz. See ESC_STATUS for higher-rate ESC data.", MavlinkFields, null)
	];

	private static readonly ImmutableArray<GeneratedMavlinkMessage> GeneratedMavlinkMessages =
	[
		new GeneratedMavlinkMessage("Namespace1", "EscInfo", GeneratedFields, SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), "EscInfo"), MavlinkMessages[0])
	];

	[Fact]
	public async Task CreateCreateInstanceMethod_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var currentNamespace = "Namespace1";

		// Act
		var methodSyntax = MavlinkMessagePayloadDeserializationGenerator.CreateCreateInstanceMethod(currentNamespace, "ESCInfoMessage", GeneratedFields);

		// Assert
		var methodCode = methodSyntax.NormalizeWhitespace().ToFullString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public async Task GenerateMessages_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var generator = new MavlinkMessageGenerator();
		var @namespace = "Namespace1";

		// Act
		var generatedMessages = MavlinkMessages
			.Select(message => generator.GenerateMavlinkMessageInternal(
				message, @namespace,
				GeneratedEnums.ToImmutableDictionary(e => e.Name, e => e)))
			.ToImmutableArray();

		// Normalize the generated code for each message
		var generatedCode = generatedMessages
			.Select(gm => gm.DeclarationSyntax.NormalizeWhitespace().ToFullString())
			.Aggregate((current, next) => current + "\n\n" + next);

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("GeneratedMessages");
	}

	[Fact]
	public async Task GenerateMessagesExtensions_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var messages = GeneratedMavlinkMessages;

		// Act
		var generatedCacheCode = MavlinkMessagesGenerator.GenerateMessageExtensions(messages);

		// Assert
		await Verify(generatedCacheCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("GeneratedMessagesCache");
	}


	[Fact]
	public async Task GenerateMavlinkMessageWithExtensions_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var message = new MavlinkMessage(
			id: 1,
			name: "SYS_STATUS",
			description: null,
			fields:
			[
				// This field is required
				new MavlinkMessageField(new MavlinkMessageFieldType("int16_t"), "current_battery",
					null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, false, null, "-1" ),
				// This field is non-required
				new MavlinkMessageField(new MavlinkMessageFieldType("int8_t"), "battery_remaining",
					null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, false, null, null, null, null, false, null, null)
			],
			deprecated: null
		);

		var generatedEnums = ImmutableArray<GeneratedMavlinkEnum>.Empty.ToImmutableDictionary(e => e.Name, e => e);
		var generator = new MavlinkMessageGenerator();
		var namespaceName = "TestNamespace";

		// Act
		var generatedMessage = generator.GenerateMavlinkMessageInternal(message, namespaceName, generatedEnums);

		var normalizedMessage = generatedMessage.DeclarationSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(normalizedMessage)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("SYS_STATUS");
	}

	[Fact]
	public async Task CreateCreateInstanceMethod_NullableFields_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var nonRequiredFields = GeneratedFields.Select(field => field with { IsRequired = false }).ToImmutableArray();

		// Act
		var methodSyntax = MavlinkMessagePayloadDeserializationGenerator.CreateCreateInstanceMethod("Namespace1", "SomeMessage", nonRequiredFields);

		// Assert
		var methodCode = methodSyntax.NormalizeWhitespace().ToFullString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public async Task GenerateMavlinkMessageInternal_ShouldAddObsoleteAttribute()
	{
		// Arrange
		const string testNamespace = "TestNamespace";

		var deprecatedInfo = new MavlinkDeprecatedInfo(
			description: "This message is deprecated.",
			since: "2023-01",
			replacedBy: "NewMessage",
			text: null
		);

		var mavlinkMessage = new MavlinkMessage(
			id: 1,
			name: "DEPRECATED_MESSAGE",
			description: null,
			fields: ImmutableArray<MavlinkMessageField>.Empty,
			deprecated: deprecatedInfo
		);

		var generatedEnums = ImmutableDictionary<string, GeneratedMavlinkEnum>.Empty;

		var generator = new MavlinkMessageGenerator();

		// Act
		var generatedMessage = generator.GenerateMavlinkMessageInternal(mavlinkMessage, testNamespace, generatedEnums);

		// Assert
		var obsoleteAttribute = generatedMessage.DeclarationSyntax.AttributeLists
			.SelectMany(attrList => attrList.Attributes)
			.FirstOrDefault(attr => attr.Name.ToString() == "System.Obsolete");

		Assert.NotNull(obsoleteAttribute);
		var obsoleteMessageArgument = obsoleteAttribute!.ArgumentList!.Arguments.First();
		var obsoleteMessage = (LiteralExpressionSyntax)obsoleteMessageArgument.Expression;
		Assert.Equal($"{deprecatedInfo}", obsoleteMessage.Token.ValueText);

		var methodCode = generatedMessage.DeclarationSyntax.NormalizeWhitespace().ToFullString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}
}
