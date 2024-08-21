namespace Shmyndra.Mavlink;

/// <summary>
/// This type describes the Packet for different versions of Mavlink. <br/>
/// Since this type can vary significantly between different versions of Mavlink, it was decided to encapsulate the implementation and handling of the original types exclusively for a specific version of Mavlink.
/// </summary>
/// <remarks>
/// This type does not have the prefix "I" as it is not used as an interface because does not describe any logic for implementation, but rather generalizes all types into one specific type.<br/>
/// It is needed to avoid using the usual "object" and to more specifically describe what the type refers to.
/// </remarks>
#pragma warning disable IDE1006 // This type must be used without the prefix "I"
public interface MavlinkPacket
#pragma warning restore IDE1006 // This type must be used without the prefix "I"
{
	// The type should be left blank
}
