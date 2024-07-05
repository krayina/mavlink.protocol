namespace MavlinkTypes;

/// <summary>
/// This type describes the Message types of the Mavlink protocol.
/// Most often used for generated types, but can also be used for manually created types.
/// </summary>
/// <remarks>
/// This type does not have the prefix "I" as it is not used as an interface because does not describe any logic for implementation, but rather generalizes all types into one specific type.<br/>
/// It is needed to avoid using the usual "object" and to more specifically describe what the type refers to.
/// </remarks>
#pragma warning disable IDE1006 // This type must be used without the prefix "I"
public interface MavlinkMessage
#pragma warning restore IDE1006 // This type must be used without the prefix "I"
{
	// The type should be left blank
}
