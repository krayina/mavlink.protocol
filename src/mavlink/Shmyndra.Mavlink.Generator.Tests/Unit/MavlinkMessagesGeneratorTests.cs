using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;

public class MavlinkMessagesGeneratorTests
{
	private const string SNAPSHOT_PATH = "..\\Snapshots/Unit/MavlinkMessageTypesGeneratorTests";

	private static readonly ImmutableList<MavlinkMessageField> _baseFields =
	[
		new(new MavlinkMessageFieldType("uint8_t"), "index", "Index of the first ESC in this message", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, 0, 60, true, null, null),
		new(new MavlinkMessageFieldType("uint64_t"), "time_usec", "Timestamp", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Us, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("uint16_t"), "counter", "Counter of data packets received", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("uint8_t"), "count", "Total number of ESCs in all messages of this type", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldEnumType("uint8_t", "ESC_CONNECTION_TYPE"), "connection_type", "Connection type protocol for all ESC", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("uint8_t"), "info", "Information regarding online/offline status of each ESC", MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("uint16_t[4]"), "failure_flags", "Bitmap of ESC failure flags", MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("uint32_t[4]"), "error_count", "Number of reported errors by each ESC since boot", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null),
		new(new MavlinkMessageFieldType("int16_t[4]"), "temperature", "Temperature of each ESC", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.CdegC, true, null, null, null, null, null, null, null)
	];

	private static readonly ImmutableList<GeneratedMavlinkMessageField> _testGeneratedFields =
	[
		new("Index", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), _baseFields[0]),
		new("TimeUsec", new GeneratedMavlinkMessageFieldType("uint64_t", "ulong"), _baseFields[1]),
		new("Counter", new GeneratedMavlinkMessageFieldType("uint16_t", "ushort"), _baseFields[2]),
		new("Count", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), _baseFields[3]),
		new("ConnectionType", new GeneratedMavlinkMessageFieldEnumType("uint8_t", "byte", new GeneratedMavlinkEnum("Namespace1", ImmutableList<GeneratedMavlinkEnumEntry>.Empty, new MavlinkEnum("ESC_CONNECTION_TYPE", null, false, ImmutableList<MavlinkEnumEntry>.Empty, null))), _baseFields[4]),
		new("Info", new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), _baseFields[5]),
		new("FailureFlags", new GeneratedMavlinkMessageFieldArrayEnumType("uint16_t[4]", "ushort", new GeneratedMavlinkEnum("Namespace1", ImmutableList<GeneratedMavlinkEnumEntry>.Empty, new MavlinkEnum("ESC_FAILURE_FLAGS", null, true, ImmutableList<MavlinkEnumEntry>.Empty, null)), 4), _baseFields[6]),
		new("ErrorCount", new GeneratedMavlinkMessageFieldArrayType("uint32_t[4]", "uint", 4), _baseFields[7]),
		new("Temperature", new GeneratedMavlinkMessageFieldArrayType("int16_t[4]", "short", 4), _baseFields[8])
	];

	[Fact]
	public async Task GenerateCreateInstanceMethod_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var currentNamespace = "Namespace1";

		// Act
		var methodSyntax = MavlinkMessagePayloadDeserializationGenerator.GenerateCreateInstanceMethod(currentNamespace, _testGeneratedFields);

		// Assert
		var methodCode = methodSyntax.NormalizeWhitespace().ToFullString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}
}
