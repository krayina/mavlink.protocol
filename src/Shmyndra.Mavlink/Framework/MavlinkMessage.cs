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

/// <summary>
/// Represents a serialization method for Mavlink messages that do not include extensions.
/// Typically used for Mavlink v1 messages.
/// </summary>
/// <remarks>
/// For more details on field reordering, see: 
/// <a href="https://mavlink.io/en/guide/serialization.html#field_reordering">MAVLink Field Reordering</a>
/// </remarks>
public interface IMavlinkMessageSerializerWithoutExtensions : MavlinkMessage
{
	/// <summary>
	/// Serializes the message into a byte array using the non-extension format.
	/// This method is applicable to Mavlink v1 messages.
	/// </summary>
	/// <returns>A byte array containing the serialized message.</returns>
	byte[] SerializeWithoutExtensions();
}

/// <summary>
/// Represents a serialization method for Mavlink messages that include extensions.
/// Typically used for Mavlink v2 messages.
/// </summary>
/// <remarks>
/// For more details on message extensions, see: 
/// <a href="https://mavlink.io/en/guide/define_xml_element.html#message_extensions">MAVLink Message Extensions</a>
/// </remarks>
public interface IMavlinkMessageSerializerWithExtensions : MavlinkMessage
{
	/// <summary>
	/// Serializes the message into a byte array using the extension format.
	/// This method is applicable to Mavlink v2 messages.
	/// </summary>
	/// <returns>A byte array containing the serialized message.</returns>
	byte[] SerializeWithExtensions();
}
