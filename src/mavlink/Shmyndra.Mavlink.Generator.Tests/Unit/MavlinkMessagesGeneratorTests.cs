using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

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
		new GeneratedMavlinkEnum("Namespace1", "EscConnectionType", null, ImmutableArray<GeneratedMavlinkEnumEntry>.Empty, SyntaxFactory.EnumDeclaration("EscConnectionType"), MavlinkEnums[0]),
		new GeneratedMavlinkEnum("Namespace1", "EscFailureFlags", null, ImmutableArray<GeneratedMavlinkEnumEntry>.Empty, SyntaxFactory.EnumDeclaration("EscFailureFlags"), MavlinkEnums[1])
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
		new GeneratedMavlinkMessageField("Index", new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"), MavlinkFields[0]),
		new GeneratedMavlinkMessageField("TimeUsec", new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "TimeUsec"), MavlinkFields[1]),
		new GeneratedMavlinkMessageField("Counter", new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "Counter"), MavlinkFields[2]),
		new GeneratedMavlinkMessageField("Count", new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Count"), MavlinkFields[3]),
		new GeneratedMavlinkMessageField("ConnectionType", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0],
				(MavlinkMessageFieldEnumType)MavlinkFields[4].Type),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "ConnectionType"), MavlinkFields[4]),
		new GeneratedMavlinkMessageField("Info", new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Info"), MavlinkFields[5]),
		new GeneratedMavlinkMessageField("FailureFlags", new GeneratedMavlinkMessageFieldArrayEnumType("ushort", GeneratedEnums[1], 4,
				(MavlinkMessageFieldEnumType)MavlinkFields[6].Type),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort[]"), "FailureFlags"), MavlinkFields[6]),
		new GeneratedMavlinkMessageField("ErrorCount", new GeneratedMavlinkMessageFieldArrayType("uint", 4, new MavlinkMessageFieldType("uint32_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint[]"), "ErrorCount"), MavlinkFields[7]),
		new GeneratedMavlinkMessageField("Temperature", new GeneratedMavlinkMessageFieldArrayType("short", 4, new MavlinkMessageFieldType("int16_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("short[]"), "Temperature"), MavlinkFields[8])
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
	public async Task GenerateMessages_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var generator = new MavlinkMessageGenerator(
			new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy()),
			new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy())
		);

		var @namespace = "Namespace1";

		// Act
		var generatedMessages = MavlinkMessages
			.Select(message => generator.GenerateMavlinkMessageInternal(
				message, @namespace,
				GeneratedEnums.ToImmutableDictionary(e => e.Original.Name, e => e)))
			.ToImmutableArray();

		// Normalize the generated code for each message
		var generatedCode = generatedMessages
			.Select(gm => gm.DeclarationSyntax.ToNormalizedString())
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

		var generatedEnums = ImmutableArray<GeneratedMavlinkEnum>.Empty.ToImmutableDictionary(e => e.Original.Name, e => e);
		var generator = new MavlinkMessageGenerator(
			new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy()),
			new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy())
		);
		var namespaceName = "TestNamespace";

		// Act
		var generatedMessage = generator.GenerateMavlinkMessageInternal(message, namespaceName, generatedEnums);

		var normalizedMessage = generatedMessage.DeclarationSyntax.ToNormalizedString();

		// Assert
		await Verify(normalizedMessage)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("SYS_STATUS");
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

		var generator = new MavlinkMessageGenerator(
			new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy()),
			new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy())
		);

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

		var recordCode = generatedMessage.DeclarationSyntax.ToNormalizedString();

		await Verify(recordCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("DEPRECATEDMESSAGE");
	}


	#region Buffer Deserialization tests

	[Fact]
	public async Task CreateBufferDeserializationMethod_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var currentNamespace = "Namespace1";
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(currentNamespace, "ESCInfoMessage", GeneratedFields);

		// Assert
		var methodCode = methodSyntax.ToNormalizedString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public void CreateBufferDeserializationMethod_ShouldEscapeReservedKeyword()
	{
		// Arrange
		var fieldType = new MavlinkMessageFieldType("ushort");

		var originalField = new MavlinkMessageField(type: fieldType, name: "fixed",
			description: null, display: default, systemUnit: default, isRequired: true, printFormat: null, increment: null, minValue: null, maxValue: null, instance: null, @default: null, invalid: null);

		var generatedType = new GeneratedMavlinkMessageFieldPrimitiveType("ushort", fieldType);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Fixed",
			generatedFieldType: generatedType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("ushort"),
				SyntaxFactory.Identifier("Fixed")
			),
			original: originalField
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var method = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "RadioMavlinkMessage",
			fields: fields
		);

		var generatedCode = method.ToNormalizedString();

		// Assert
		Assert.Contains("var @fixed =", generatedCode);
		Assert.Contains("Fixed = @fixed", generatedCode);
	}

	[Fact]
	public void CreateBufferDeserializationMethod_ShouldAvoidNameConflictsByAddingUnderscore()
	{
		// Arrange
		var fieldType = new MavlinkMessageFieldType("uint8_t");

		var generatedType = new GeneratedMavlinkMessageFieldPrimitiveType("byte", fieldType);

		var originalField = new MavlinkMessageField(
			type: fieldType, name: "Payload",
			description: null, display: default, systemUnit: default, isRequired: true, printFormat: null, increment: null, minValue: null, maxValue: null, instance: null, @default: null, invalid: null
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Payload",
			generatedFieldType: generatedType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("test"),
				SyntaxFactory.Identifier("Payload")
			),
			original: originalField
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var method = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "FileTransferProtocolMavlinkMessage",
			fields: fields
		);

		var generatedCode = method.NormalizeWhitespace().ToFullString();

		// Assert
		Assert.Contains("var payloadLocal =", generatedCode);
		Assert.Contains("Payload = payloadLocal", generatedCode);
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_ShouldGenerateNullableEnumDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldEnumType(
			ConvertedType: "uint",
			GeneratedEnum: new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "MavSysStatusSensorExtended",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("MavSysStatusSensorExtended"),
				original: new MavlinkEnum(
					name: "MavSysStatusSensorExtended",
					description: null,
					bitmask: null,
					entries: ImmutableArray<MavlinkEnumEntry>.Empty,
					deprecated: null)
			),
			Original: new MavlinkMessageFieldEnumType("uint", "MavSysStatusSensorExtended")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "OnboardControlSensorsPresentExtended",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("MavSysStatusSensorExtended?"),
				SyntaxFactory.Identifier("OnboardControlSensorsPresentExtended")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldEnumType("uint", "MavSysStatusSensorExtended"),
				name: "OnboardControlSensorsPresentExtended",
				description: null, display: default, systemUnit: default,
				isRequired: false, // Nullable
				printFormat: null, increment: null, minValue: null, maxValue: null, instance: null, @default: null, invalid: null)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_NullableFields_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var nonRequiredFields = GeneratedFields.Select(field => field with { Original = field.Original with { IsRequired = false } }).ToImmutableArray();
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal("Namespace1", "SomeMessage", nonRequiredFields);

		// Assert
		var methodCode = methodSyntax.ToNormalizedString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldEnumType(
			ConvertedType: "byte",
			GeneratedEnum: new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "LimitModule",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("LimitModule"),
				original: new MavlinkEnum(
					name: "LimitModule",
					description: null,
					bitmask: null,
					entries: ImmutableArray<MavlinkEnumEntry>.Empty,
					deprecated: null)
			),
			Original: new MavlinkMessageFieldEnumType("byte", "LimitModule")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "ModsEnabled",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("enumType"),
				SyntaxFactory.Identifier("enumType")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldEnumType("byte", "LimitModule"),
				name: "enumType",
				description: null, display: MavlinkMessageFieldDisplay.Bitmask, systemUnit: default,
				isRequired: false, // Nullable
				printFormat: null, increment: null, minValue: null, maxValue: null, instance: null, @default: null, invalid: null)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_WithEnumArrayWithoutBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldArrayEnumType(
			ConvertedType: "ushort",
			GeneratedEnum: new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "MavCmd",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("MavCmd"),
				original: new MavlinkEnum(
					name: "MAV_CMD",
					description: null,
					bitmask: null,
					entries: ImmutableArray<MavlinkEnumEntry>.Empty,
					deprecated: null)
			),
			ArrayLength: 5,
			Original: new MavlinkMessageFieldEnumType("uint16", "MAV_CMD")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Command",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("command"),
				SyntaxFactory.Identifier("test")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldType("ushort"),
				name: "test",
				description: null, display: MavlinkMessageFieldDisplay.None, systemUnit: default,
				isRequired: true,
				printFormat: null, increment: null, minValue: null, maxValue: null, instance: null, @default: null, invalid: null)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_WithInvalidFloat_ShouldGenerateDeserialization()
	{
		// Arrange
		var floatGeneratedField = ImmutableArray.Create(new GeneratedMavlinkMessageField("SomeFloat", new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "SomeFloat"), MavlinkFields[0] with { Invalid = "NaN" }));
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: floatGeneratedField
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateBufferDeserializationMethod_WithInvalidIntegers_ShouldGenerateDeserialization()
	{
		// Arrange
		var floatGeneratedField = ImmutableArray.CreateRange(new[]
		{
			new GeneratedMavlinkMessageField("SomeUshort",
				new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUshort"),
				MavlinkFields[0] with { Invalid = "UINT16_MAX" }),
			new GeneratedMavlinkMessageField("SomeShort",
				new GeneratedMavlinkMessageFieldPrimitiveType("short", new MavlinkMessageFieldType("int16_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("short"), "SomeShort"),
				MavlinkFields[0] with { Invalid = "INT16_MAX" }),
			new GeneratedMavlinkMessageField("SomeByte",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
				MavlinkFields[0] with { Invalid = "UINT8_MAX" }),
			new GeneratedMavlinkMessageField("SomeSbyte",
				new GeneratedMavlinkMessageFieldPrimitiveType("sbyte", new MavlinkMessageFieldType("int8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("sbyte"), "SomeSbyte"),
				MavlinkFields[0] with { Invalid = "INT8_MAX" })
		});

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkBufferDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: floatGeneratedField
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithByteBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeByte",
			new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someByte", fieldName);
		Assert.Contains("var someByteValue = payload[0];", code);
		Assert.Contains("var someByte = new ByteBitmask((byte)someByteValue);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithUIntBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUInt",
			new GeneratedMavlinkMessageFieldPrimitiveType("uint", new MavlinkMessageFieldType("uint32_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint"), "SomeUInt"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUInt", fieldName);
		Assert.Contains("var someUIntValue = BitConverter.ToUInt32(payload, 0);", code);
		Assert.Contains("var someUInt = new UIntBitmask((uint)someUIntValue);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithULongBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeULong",
			new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "SomeULong"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someULong", fieldName);
		Assert.Contains("var someULongValue = BitConverter.ToUInt64(payload, 0);", code);
		Assert.Contains("var someULong = new ULongBitmask((ulong)someULongValue);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithUShortBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUShort", fieldName);
		Assert.Contains("var someUShortValue = BitConverter.ToUInt16(payload, 0);", code);
		Assert.Contains("var someUShort = new UShortBitmask((ushort)someUShortValue);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithUShortBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldArrayType("ushort", 4, new MavlinkMessageFieldType("uint16_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUShortArray", fieldName);
		Assert.Contains(@"
var tempSomeUShort = new UShortBitmask[4];
for (int idxSomeUShort = 0; idxSomeUShort < 4; idxSomeUShort++)
{
    int elementOffset = 0 + idxSomeUShort * 2;
    var value = BitConverter.ToUInt16(payload, elementOffset);
    tempSomeUShort[idxSomeUShort] = new UShortBitmask((ushort)value);
}
var someUShortArray = System.Collections.Immutable.ImmutableArray.CreateRange(tempSomeUShort);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnum",
			new GeneratedMavlinkMessageFieldEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someEnum", fieldName);
		Assert.Contains("var someEnumValue = BitConverter.ToUInt16(payload, 0);", code);
		Assert.Contains("var someEnum = new SomeEnumFlagsBitmask((ushort)someEnumValue);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldDeserialization_WithEnumBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnumArray",
			new GeneratedMavlinkMessageFieldArrayEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				4,
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someEnumArrayArray", fieldName);
		Assert.Contains(@"
var tempSomeEnumArray = new SomeEnumFlagsBitmask[4];
for (int idxSomeEnumArray = 0; idxSomeEnumArray < 4; idxSomeEnumArray++)
{
    int elementOffset = 0 + idxSomeEnumArray * 2;
    var value = BitConverter.ToUInt16(payload, elementOffset);
    tempSomeEnumArray[idxSomeEnumArray] = new SomeEnumFlagsBitmask((ushort)value);
}
var someEnumArrayArray = System.Collections.Immutable.ImmutableArray.CreateRange(tempSomeEnumArray);", code);
	}

	#endregion

	#region Span Deserialization tests

	[Fact]
	public async Task CreateSpanDeserializationMethod_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var currentNamespace = "Namespace1";
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(currentNamespace, "ESCInfoMessage", GeneratedFields);

		// Assert
		var methodCode = methodSyntax.ToNormalizedString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public void CreateSpanDeserializationMethod_ShouldEscapeReservedKeyword()
	{
		// Arrange
		var fieldType = new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t"));
		var originalField = new MavlinkMessageField(
			type: new MavlinkMessageFieldType("uint16_t"),
			name: "fixed",
			description: null,
			display: default,
			systemUnit: default,
			isRequired: true,
			printFormat: null,
			increment: null,
			minValue: null,
			maxValue: null,
			instance: null,
			@default: null,
			invalid: null);
		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Fixed",
			generatedFieldType: fieldType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("ushort"),
				SyntaxFactory.Identifier("Fixed")),
			original: originalField);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var method = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "RadioMavlinkMessage",
			fields: fields
		);

		var generatedCode = method.ToNormalizedString();

		// Assert
		Assert.Contains("var @fixed =", generatedCode);
		Assert.Contains("Fixed = @fixed", generatedCode);
	}

	[Fact]
	public void CreateSpanDeserializationMethod_ShouldAvoidNameConflictsByAddingUnderscore()
	{
		// Arrange
		var fieldType = new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t"));

		var originalField = new MavlinkMessageField(
			type: new MavlinkMessageFieldType("uint8_t"),
			name: "Payload",
			description: null,
			display: default,
			systemUnit: default,
			isRequired: true,
			printFormat: null,
			increment: null,
			minValue: null,
			maxValue: null,
			instance: null,
			@default: null,
			invalid: null
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Payload",
			generatedFieldType: fieldType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("test"),
				SyntaxFactory.Identifier("Payload")
			),
			original: originalField
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var method = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "FileTransferProtocolMavlinkMessage",
			fields: fields
		);

		var generatedCode = method.NormalizeWhitespace().ToFullString();

		// Assert
		Assert.Contains("var payloadLocal =", generatedCode);
		Assert.Contains("Payload = payloadLocal", generatedCode);
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_ShouldGenerateNullableEnumDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldEnumType(
			"uint",
			new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "MavSysStatusSensorExtended",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("MavSysStatusSensorExtended"),
				original: new MavlinkEnum(
					name: "MavSysStatusSensorExtended",
					description: null,
					bitmask: null,
					entries: ImmutableArray<MavlinkEnumEntry>.Empty,
					deprecated: null)
			),
			new MavlinkMessageFieldEnumType("uint", "MavSysStatusSensorExtended")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "OnboardControlSensorsPresentExtended",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("MavSysStatusSensorExtended?"),
				SyntaxFactory.Identifier("OnboardControlSensorsPresentExtended")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldEnumType("uint", "MavSysStatusSensorExtended"),
				name: "OnboardControlSensorsPresentExtended",
				description: null,
				display: default,
				systemUnit: default,
				isRequired: false,
				printFormat: null,
				increment: null,
				minValue: null,
				maxValue: null,
				instance: null,
				@default: null,
				invalid: null)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_NullableFields_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var nonRequiredFields = GeneratedFields.Select(field => field with { Original = field.Original with { IsRequired = false } }).ToImmutableArray();
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal("Namespace1", "SomeMessage", nonRequiredFields);

		// Assert
		var methodCode = methodSyntax.ToNormalizedString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldEnumType(
			"byte",
			new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "LimitModule",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("LimitModule"),
				original: new MavlinkEnum(
					name: "LimitModule",
					description: null,
					bitmask: null,
					entries: ImmutableArray<MavlinkEnumEntry>.Empty,
					deprecated: null)
			),
			new MavlinkMessageFieldEnumType("byte", "LimitModule")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "ModsEnabled",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("LimitModule?"),
				SyntaxFactory.Identifier("ModsEnabled")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldEnumType("byte", "LimitModule"),
				name: "ModsEnabled",
				description: null,
				display: MavlinkMessageFieldDisplay.Bitmask,
				systemUnit: default,
				isRequired: false,
				printFormat: null,
				increment: null,
				minValue: null,
				maxValue: null,
				instance: null,
				@default: null,
				invalid: null
			)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_WithEnumArrayWithoutBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var enumType = new GeneratedMavlinkMessageFieldArrayEnumType(
			"ushort",
			new GeneratedMavlinkEnum(
				@namespace: "TestNamespace",
				generatedName: "MavCmd",
				null,
				generatedEntries: ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
				declarationSyntax: SyntaxFactory.EnumDeclaration("MavCmd"),
				original: new MavlinkEnum("MAV_CMD", null, null, ImmutableArray<MavlinkEnumEntry>.Empty, null)
			),
			5,
			new MavlinkMessageFieldEnumType("uint16", "MAV_CMD")
		);

		var generatedField = new GeneratedMavlinkMessageField(
			generatedName: "Command",
			generatedFieldType: enumType,
			declarationSyntax: SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("command"),
				SyntaxFactory.Identifier("test")
			),
			original: new MavlinkMessageField(
				type: new MavlinkMessageFieldEnumType("uint16", "MAV_CMD"),
				name: "test",
				description: null,
				display: MavlinkMessageFieldDisplay.None,
				systemUnit: default,
				isRequired: true,
				printFormat: null,
				increment: null,
				minValue: null,
				maxValue: null,
				instance: null,
				@default: null,
				invalid: null
			)
		);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());
		var fields = ImmutableArray.Create(generatedField);

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: fields
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_WithInvalidFloat_ShouldGenerateDeserialization()
	{
		// Arrange
		var floatGeneratedField = ImmutableArray.Create(new GeneratedMavlinkMessageField("SomeFloat", new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "SomeFloat"), MavlinkFields[0] with { Invalid = "NaN" }));
		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: floatGeneratedField
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateSpanDeserializationMethod_WithInvalidIntegers_ShouldGenerateDeserialization()
	{
		// Arrange
		var floatGeneratedField = ImmutableArray.CreateRange(
		[
			new GeneratedMavlinkMessageField("SomeUshort",
				new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUshort"),
				MavlinkFields[0] with { Invalid = "UINT16_MAX" }),
			new GeneratedMavlinkMessageField("SomeShort",
				new GeneratedMavlinkMessageFieldPrimitiveType("short", new MavlinkMessageFieldType("int16")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("short"), "SomeShort"),
				MavlinkFields[0] with { Invalid = "INT16_MAX" }),
			new GeneratedMavlinkMessageField("SomeByte",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
				MavlinkFields[0] with { Invalid = "UINT8_MAX" }),
			new GeneratedMavlinkMessageField("SomeSbyte",
				new GeneratedMavlinkMessageFieldPrimitiveType("sbyte", new MavlinkMessageFieldType("int8")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("sbyte"), "SomeSbyte"),
				MavlinkFields[0] with { Invalid = "INT8_MAX" })
		]);

		var deserializationMethodGenerator = new MavlinkMessageDeserializationMethodGenerator(new MavlinkSpanDeserializationGeneratorStrategy());

		// Act
		var methodSyntax = deserializationMethodGenerator.CreateDeserializeWithoutExtensionsMethodInternal(
			@namespace: "TestNamespace",
			messageName: "SysStatusMavlinkMessage",
			fields: floatGeneratedField
		);

		var generatedCode = methodSyntax.ToNormalizedString();

		// Assert
		await Verify(generatedCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("Test");
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithByteBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeByte",
			new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someByte", fieldName);
		Assert.Contains("var someByte = new ByteBitmask((byte)payload[0]);\r\n", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithUIntBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUInt",
			new GeneratedMavlinkMessageFieldPrimitiveType("uint", new MavlinkMessageFieldType("uint32_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint"), "SomeUInt"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUInt", fieldName);
		Assert.Contains("var someUInt = new UIntBitmask((uint)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)));", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithULongBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeULong",
			new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "SomeULong"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someULong", fieldName);
		Assert.Contains("var someULong = new ULongBitmask((ulong)System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(payload.Slice(0, 8)));", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithUShortBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUShort", fieldName);
		Assert.Contains("var someUShort = new UShortBitmask((ushort)System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2)));", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithUShortBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldArrayType("ushort", 4, new MavlinkMessageFieldType("uint16_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someUShortArray", fieldName);
		Assert.Contains(@"
var tempSomeUShort = new UShortBitmask[4];
for (int idxSomeUShort = 0; idxSomeUShort < 4; idxSomeUShort++)
{
    int elementOffset = 0 + idxSomeUShort * 2;
    var value = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(elementOffset, 2));
    tempSomeUShort[idxSomeUShort] = new UShortBitmask((ushort)value);
}
var someUShortArray = System.Collections.Immutable.ImmutableArray.CreateRange(tempSomeUShort);", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnum",
			new GeneratedMavlinkMessageFieldEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someEnum", fieldName);
		Assert.Contains("var someEnum = new SomeEnumFlagsBitmask((ushort)System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2)));", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldDeserialization_WithEnumBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnumArray",
			new GeneratedMavlinkMessageFieldArrayEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				4,
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var deserializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanDeserializationStrategy();

		// Act
		var fieldName = deserializationFieldGenerator.DeserializeField(
			sb, bitmaskField, ref offset, "TestNamespace", "payload");

		var code = sb.ToString();

		// Assert
		Assert.Contains("someEnumArrayArray", fieldName);
		Assert.Contains(@"
var tempSomeEnumArray = new SomeEnumFlagsBitmask[4];
for (int idxSomeEnumArray = 0; idxSomeEnumArray < 4; idxSomeEnumArray++)
{
    int elementOffset = 0 + idxSomeEnumArray * 2;
    var value = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(elementOffset, 2));
    tempSomeEnumArray[idxSomeEnumArray] = new SomeEnumFlagsBitmask((ushort)value);
}
var someEnumArrayArray = System.Collections.Immutable.ImmutableArray.CreateRange(tempSomeEnumArray);", code);
	}

	#endregion

	#region Buffer Serialization tests

	[Fact]
	public async Task GenerateSerializeBufferMethodWithoutExtensions_WhenNoExtensions_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkBufferSerializationGeneratorStrategy());
		var testFields = GeneratedFields;

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithoutExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeBufferMethodWithExtensions_WhenHasOneExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkBufferSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank",
					"",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false,
					null,
					null,
					null,
					null,
					null,
					null,
					null)
			)
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeBufferMethodWithExtensions_WhenHasOneSimpleExtensionAndCollectionExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkBufferSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldArrayType("ulong", 4, new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint64_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeBufferMethodWithExtensions_WhenHasOneSimpleExtensionAndEnumExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkBufferSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldEnumType("ulong",
					new GeneratedMavlinkEnum(
						"TestNamespace", "TestEnum", null,
						ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
						SyntaxFactory.EnumDeclaration("Test"),
						new MavlinkEnum("Test", null, null, ImmutableArray<MavlinkEnumEntry>.Empty, null)),
					new MavlinkMessageFieldEnumType("uint64_t", "TestEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldEnumType("uint64_t", "TestEnum"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeBufferMethodWithExtensions_WhenHasOneSimpleExtensionAndEnumArrayExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkBufferSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("FailureFlags",
				new GeneratedMavlinkMessageFieldArrayEnumType("ushort", GeneratedEnums[1], 4, new MavlinkMessageFieldEnumType("uint16_t[4]", "ESC_FAILURE_FLAGS")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[6]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldArrayEnumType("ushort", GeneratedEnums[1], 4, new MavlinkMessageFieldEnumType("uint16_t[4]", "ESC_FAILURE_FLAGS")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[6] with { IsRequired = false })
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithByteBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeByte",
			new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("BitConverter.GetBytes(SomeByte.Bitmask).CopyTo(buffer, 0);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithUIntBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUInt",
			new GeneratedMavlinkMessageFieldPrimitiveType("uint", new MavlinkMessageFieldType("uint32_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint"), "SomeUInt"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("BitConverter.GetBytes(SomeUInt.Bitmask).CopyTo(buffer, 0);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithULongBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeULong",
			new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "SomeULong"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("BitConverter.GetBytes(SomeULong.Bitmask).CopyTo(buffer, 0);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithUShortBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("BitConverter.GetBytes(SomeUShort.Bitmask).CopyTo(buffer, 0);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithUShortBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldArrayType("ushort", 4, new MavlinkMessageFieldType("uint16_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains(@"var serializedSomeUShort = new ushort[4];
for (int i = 0; i < 4; i++)
{
    serializedSomeUShort[i] = SomeUShort[i].Bitmask;
}
Buffer.BlockCopy(serializedSomeUShort, 0, buffer, 0, 8);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnum",
			new GeneratedMavlinkMessageFieldEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("BitConverter.GetBytes(SomeEnum.Bitmask).CopyTo(buffer, 0);", code);
	}

	[Fact]
	public async Task CreateBufferBitmaskFieldSerialization_WithEnumBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnumArray",
			new GeneratedMavlinkMessageFieldArrayEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				4,
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldBufferSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains(@"var serializedSomeEnumArray = new ushort[4];
for (int i = 0; i < 4; i++)
{
    serializedSomeEnumArray[i] = SomeEnumArray[i].Bitmask;
}
Buffer.BlockCopy(serializedSomeEnumArray, 0, buffer, 0, 8);", code);
	}

	#endregion

	#region Span Serialization tests

	[Fact]
	public async Task GenerateSerializeSpanMethodWithoutExtensions_WhenNoExtensions_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy());
		var testFields = GeneratedFields;

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithoutExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeSpanMethodWithExtensions_WhenHasOneExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None,
					MavlinkSystemUnit.Empty,
					false,
					null, null, null, null, null, null, null)
			)
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeSpanMethodWithExtensions_WhenHasOneSimpleExtensionAndTwoCollectionExtensions_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldArrayType("ulong", 4, new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint64_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("ThirdExtension",
				new GeneratedMavlinkMessageFieldArrayType("ulong", 4, new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint64_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeSpanMethodWithExtensions_WhenHasOneSimpleExtensionAndEnumExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("TimeUsec",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint64_t"), "TimeUsec"),
				MavlinkFields[1]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldEnumType("ulong",
					new GeneratedMavlinkEnum(
						"TestNamespace", "TestEnum", null,
						ImmutableArray<GeneratedMavlinkEnumEntry>.Empty,
						SyntaxFactory.EnumDeclaration("Test"),
						new MavlinkEnum("Test", null, null, ImmutableArray<MavlinkEnumEntry>.Empty, null)),
					new MavlinkMessageFieldEnumType("uint64_t", "TestEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldEnumType("uint64_t", "TestEnum"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}

	[Fact]
	public async Task GenerateSerializeSpanMethodWithExtensions_WhenHasOneSimpleExtensionAndEnumArrayExtension_ShouldMatchSnapshot()
	{
		// Arrange
		var serializeMethodGenerator = new MavlinkMessageSerializationMethodGenerator(new MavlinkSpanSerializationGeneratorStrategy());
		var testFields = new List<GeneratedMavlinkMessageField>()
		{
			new GeneratedMavlinkMessageField("Index",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Index"),
				MavlinkFields[0]),
			new GeneratedMavlinkMessageField("FailureFlags",
				new GeneratedMavlinkMessageFieldArrayEnumType("ushort", GeneratedEnums[1], 4, new MavlinkMessageFieldEnumType("uint16_t[4]", "ESC_FAILURE_FLAGS")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort[]"), "Index"),
				MavlinkFields[6]),
			new GeneratedMavlinkMessageField("FirstExtension",
				new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Bank"),
				new MavlinkMessageField(
					new MavlinkMessageFieldType("uint8_t"),
					"Bank", "",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty,
					false, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("SecondExtension",
				new GeneratedMavlinkMessageFieldArrayEnumType("ushort", GeneratedEnums[1], 4, new MavlinkMessageFieldEnumType("uint16_t[4]", "ESC_FAILURE_FLAGS")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort[]"), "Index"),
				MavlinkFields[6] with { IsRequired = false })
		}.ToImmutableArray();

		// Act
		var methodSyntax = serializeMethodGenerator.CreateSerializeWithExtensionsMethodInternal(testFields);
		var generatedCode = methodSyntax.NormalizeWhitespace().ToFullString();

		// Assert
		await Verify(generatedCode)
		  .UseDirectory(SNAPSHOT_PATH)
		  .UseParameters("TestMavlinkMessage");
	}


	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithByteBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeByte",
			new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SomeByte"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("finalSpan[0] = SomeByte.Bitmask;", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithUIntBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUInt",
			new GeneratedMavlinkMessageFieldPrimitiveType("uint", new MavlinkMessageFieldType("uint32_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint"), "SomeUInt"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(finalSpan.Slice(0, 4), SomeUInt.Bitmask);", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithULongBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeULong",
			new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "SomeULong"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(finalSpan.Slice(0, 8), SomeULong.Bitmask);", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithUShortBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice(0, 2), SomeUShort.Bitmask);", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithUShortBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
		var bitmaskField = new GeneratedMavlinkMessageField("SomeUShort",
			new GeneratedMavlinkMessageFieldArrayType("ushort", 4, new MavlinkMessageFieldType("uint16_t[4]")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeUShort"),
			MavlinkFields[0] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains(@"for (int i = 0; i < 4; i++)
{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice(0 + i * 2, 2), SomeUShort[i].Bitmask);
}", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithEnumBitmask_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnum",
			new GeneratedMavlinkMessageFieldEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains("System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice(0, 2), SomeEnum.Bitmask);", code);
	}

	[Fact]
	public async Task CreateSpanBitmaskFieldSerialization_WithEnumBitmaskArray_ShouldGenerateDeserialization()
	{
		// Arrange
		var sb = new StringBuilder();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var bitmaskField = new GeneratedMavlinkMessageField("SomeEnumArray",
			new GeneratedMavlinkMessageFieldArrayEnumType("ushort",
				new GeneratedMavlinkEnum("TestNamespace", "SomeEnumFlags", null, [], null, null),
				4,
				new MavlinkMessageFieldEnumType("uint16_t", "SomeEnumFlags")),
			SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "SomeEnum"),
			MavlinkFields[4] with { Display = MavlinkMessageFieldDisplay.Bitmask }); // Bitmask field
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		int offset = 0;

		var serializationFieldGenerator = new MavlinkObjectiveBitmaskFieldSpanSerializationStrategy();

		// Act
		serializationFieldGenerator.SerializeField(sb, bitmaskField, ref offset);

		var code = sb.ToString();

		// Assert
		Assert.Contains(@"for (int i = 0; i < 4; i++)
{
    System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(finalSpan.Slice(0 + i * 2, 2), SomeEnumArray[i].Bitmask);
}", code);
	}

	#endregion


	[Fact]
	public async Task GenerateMavlinkTypes_GeneratedSimpleCrc_ShouldBe152()
	{
		// Arrange
		var fields = ImmutableArray.Create(
			new GeneratedMavlinkMessageField("target_system",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "TargetSystem"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "target_system", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("target_component",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "TargetComponent"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "target_component", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("command",
				new GeneratedMavlinkMessageFieldEnumType("ushort", GeneratedEnums[0], new MavlinkMessageFieldEnumType("uint16_t", "command")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "Command"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint16_t"), "command", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("confirmation",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Confirmation"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "confirmation", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param1",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param1"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param1", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param2",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param2"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param2", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param3",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param3"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param3", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param4",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param4"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param4", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param5",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param5"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param5", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param6",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param6"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param6", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param7",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param7"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param7", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null))
		).ToImmutableArray();

		var mavlinkGeneratedMessages = new List<GeneratedMavlinkMessage>
		{
			new GeneratedMavlinkMessage("testNamespace", "CommandLong", fields, SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), "CommandLong"),
				new MavlinkMessage(76U, "COMMAND_LONG", null, ImmutableArray<MavlinkMessageField>.Empty, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = MavlinkMessagesGenerator.GenerateMessageExtensions(mavlinkGeneratedMessages);

		// Assert
		Assert.Contains("\"COMMAND_LONG\", 33, 33, 152", methodSyntax);
	}

	[Fact]
	public void GenerateMavlinkTypes_GenerateCrcByExtensionField_ShouldBe38()
	{
		// Arrange
		var fields = ImmutableArray.Create(
			new GeneratedMavlinkMessageField("target_system",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "TargetSystem"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "target_system", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("target_component",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "TargetComponent"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "target_component", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("seq",
				new GeneratedMavlinkMessageFieldPrimitiveType("ushort", new MavlinkMessageFieldType("uint16_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "Seq"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint16_t"), "seq", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("frame",
				new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Frame"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "frame", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("command",
				new GeneratedMavlinkMessageFieldEnumType("ushort", GeneratedEnums[0], new MavlinkMessageFieldEnumType("ushort", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ushort"), "Command"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint16_t"), "command", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("current",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Current"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "current", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("autocontinue",
				new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Autocontinue"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "autocontinue", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param1",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param1"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param1", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param2",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param2"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param2", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param3",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param3"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param3", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("param4",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Param4"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "param4", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("x",
				new GeneratedMavlinkMessageFieldPrimitiveType("int", new MavlinkMessageFieldType("int32_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "X"),
				new MavlinkMessageField(new MavlinkMessageFieldType("int32_t"), "x", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("y",
				new GeneratedMavlinkMessageFieldPrimitiveType("int", new MavlinkMessageFieldType("int32_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "Y"),
				new MavlinkMessageField(new MavlinkMessageFieldType("int32_t"), "y", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("z",
				new GeneratedMavlinkMessageFieldPrimitiveType("float", new MavlinkMessageFieldType("float")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float"), "Z"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float"), "z", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("mission_type",
				new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "MissionType"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "mission_type", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, false, null, null, null, null, null, null, null))
		).ToImmutableArray();

		var mavlinkGeneratedMessages = new List<GeneratedMavlinkMessage>
		{
			new GeneratedMavlinkMessage("testNamespace", "MissionItemInt", fields, SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), "MissionItemInt"),
				new MavlinkMessage(73U, "MISSION_ITEM_INT", null, ImmutableArray<MavlinkMessageField>.Empty, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = MavlinkMessagesGenerator.GenerateMessageExtensions(mavlinkGeneratedMessages);

		// Assert
		Assert.Contains("\"MISSION_ITEM_INT\", 37, 38, 38", methodSyntax);
	}

	[Fact]
	public async Task GenerateMavlinkTypes_GeneratedCrcByArrayField_ShouldBe47()
	{
		// Arrange
		var fields = ImmutableArray.Create(
			new GeneratedMavlinkMessageField("time_usec", new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")), SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("ulong"), "TimeUsec"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint64_t"),
					"time_usec", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("controls", new GeneratedMavlinkMessageFieldArrayType("float", 16, new MavlinkMessageFieldEnumType("float", "SomeEnum")), SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("float[]"), "Controls"),
				new MavlinkMessageField(new MavlinkMessageFieldType("float[16]"),
					"controls", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("mode", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")), SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Mode"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"),
					"mode", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("flags", new GeneratedMavlinkMessageFieldPrimitiveType("ulong", new MavlinkMessageFieldType("uint64_t")), SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Flags"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint64_t"),
					"flags", null, MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			)
		).ToImmutableArray();

		var mavlinkGeneratedMessages = new List<GeneratedMavlinkMessage>
		{
			new GeneratedMavlinkMessage("testNamespace", "HilActuatorControls", fields, SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), "HilActuatorControls"),
				new MavlinkMessage(93U, "HIL_ACTUATOR_CONTROLS", null, ImmutableArray<MavlinkMessageField>.Empty, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = MavlinkMessagesGenerator.GenerateMessageExtensions(mavlinkGeneratedMessages);

		// Assert
		Assert.Contains("\"HIL_ACTUATOR_CONTROLS\", 81, 81, 47", methodSyntax);
	}

	[Fact]
	public void GenerateMavlinkTypes_GeneratedCrcByMavlinkVersion_ShouldBe50()
	{
		// Arrange
		var fields = ImmutableArray.Create(
			new GeneratedMavlinkMessageField("type", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Type"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "type", "MAV_TYPE",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("autopilot", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "Autopilot"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "autopilot", "MAV_AUTOPILOT",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("base_mode", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "BaseMode"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "base_mode", "MAV_MODE_FLAG",
					MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("custom_mode", new GeneratedMavlinkMessageFieldPrimitiveType("uint", new MavlinkMessageFieldType("uint32_t")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("uint"), "CustomMode"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint32_t"), "custom_mode", null,
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("system_status", new GeneratedMavlinkMessageFieldEnumType("byte", GeneratedEnums[0], new MavlinkMessageFieldEnumType("byte", "SomeEnum")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "SystemStatus"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t"), "system_status", "MAV_STATE",
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			),
			new GeneratedMavlinkMessageField("mavlink_version", new GeneratedMavlinkMessageFieldPrimitiveType("byte", new MavlinkMessageFieldType("uint8_t_mavlink_version")),
				SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("byte"), "MavlinkVersion"),
				new MavlinkMessageField(new MavlinkMessageFieldType("uint8_t_mavlink_version"), "mavlink_version", null,
					MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.A, isRequired: true,
					null, null, null, null, null, null, null)
			)
		);

		var mavlinkGeneratedMessages = new List<GeneratedMavlinkMessage>
		{
			new GeneratedMavlinkMessage("testNamespace", "Heartbeat", fields,
				SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), "Heartbeat"),
				new MavlinkMessage(0U, "HEARTBEAT", null, ImmutableArray<MavlinkMessageField>.Empty, null))
		}.ToImmutableArray();

		// Act
		var methodSyntax = MavlinkMessagesGenerator.GenerateMessageExtensions(mavlinkGeneratedMessages);

		// Assert
		Assert.Contains("\"HEARTBEAT\", 9, 9, 50", methodSyntax);
	}
}
