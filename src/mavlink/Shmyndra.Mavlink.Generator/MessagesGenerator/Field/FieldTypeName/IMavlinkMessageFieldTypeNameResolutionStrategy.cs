namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a contract for resolving the type name for a specific category of MAVLink fields.
/// </summary>
public interface IMavlinkMessageFieldTypeNameResolutionStrategy
{
	string ResolvePrimitive(MavlinkMessageField field);
	string ResolveEnum(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string currentNamespace);
}
