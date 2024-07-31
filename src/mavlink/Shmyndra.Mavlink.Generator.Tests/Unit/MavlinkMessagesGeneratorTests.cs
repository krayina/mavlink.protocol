using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator.Tests.Unit;
public class MavlinkMessagesGeneratorTests
{
	private const string SNAPSHOT_PATH = "..\\Snapshots/Unit/MavlinkMessageTypesGeneratorTests";

	[Fact]
	public async Task GenerateCreateInstanceMethod_ShouldMatchExpectedSnapshot()
	{
		// Arrange
		var fields = new List<GeneratedMavlinkMessageField>
		{
			new GeneratedMavlinkMessageField("Index", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), "index", "Index of the first ESC in this message",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, 0, 60, true, null, null)),
			new GeneratedMavlinkMessageField("TimeUsec", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint64_t", "ulong"), "time_usec", "Timestamp",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Us, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("Counter", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint16_t", "ushort"), "counter", "Counter of data packets received",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("Count", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), "count", "Total number of ESCs in all messages of this type",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("ConnectionType", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldEnumType("uint8_t", "byte",
					new GeneratedMavlinkEnum("Namespace1", ImmutableList<GeneratedMavlinkEnumEntry>.Empty,
						new MavlinkEnum("ESC_CONNECTION_TYPE", null, false, ImmutableList<MavlinkEnumEntry>.Empty, null))),
				"connection_type", "Connection type protocol for all ESC", MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("Info", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint8_t", "byte"), "info", "Information regarding online/offline status of each ESC",
				MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("FailureFlags", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldArrayEnumType("uint16_t[4]", "ushort",
					new GeneratedMavlinkEnum("Namespace1", ImmutableList<GeneratedMavlinkEnumEntry>.Empty,
						new MavlinkEnum("ESC_FAILURE_FLAGS", null, true, ImmutableList<MavlinkEnumEntry>.Empty, null)), 4),
				"failure_flags", "Bitmap of ESC failure flags", MavlinkMessageFieldDisplay.Bitmask, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("ErrorCount", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("uint32_t[4]", "uint"), "error_count", "Number of reported errors by each ESC since boot",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.Empty, true, null, null, null, null, null, null, null)),
			new GeneratedMavlinkMessageField("Temperature", new MavlinkMessageField(
				new GeneratedMavlinkMessageFieldType("int16_t[4]", "short"), "temperature", "Temperature of each ESC",
				MavlinkMessageFieldDisplay.None, MavlinkSystemUnit.CdegC, true, null, null, null, null, null, null, null))
		}.ToImmutableList();

		var currentNamespace = "Namespace1";

		// Act
		var methodSyntax = MavlinkMessagePayloadDeserializationGenerator.GenerateCreateInstanceMethod(currentNamespace, fields);

		// Assert
		var methodCode = methodSyntax.NormalizeWhitespace().ToFullString();

		await Verify(methodCode)
			.UseDirectory(SNAPSHOT_PATH)
			.UseParameters("ESCInfoMessage");
	}
}
