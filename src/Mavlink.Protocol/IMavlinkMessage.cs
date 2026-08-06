namespace Mavlink;

/// <summary>
/// Root marker for every generated MAVLink message DTO.
///
/// Carries no members by design: everything the runtime needs about a message
/// (id, CRC extra, payload lengths, serialization) lives in its generated
/// *MessageInfo companion, never on the DTO. The interface exists so generic
/// APIs can constrain on "this is a MAVLink message", and so boxed flows
/// (queues, SubscribeAll round-trips) have a common type to pass around.
/// </summary>
public interface IMavlinkMessage { }
