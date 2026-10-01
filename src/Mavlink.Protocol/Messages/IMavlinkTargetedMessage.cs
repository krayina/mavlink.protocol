namespace Mavlink;

/// <summary>
/// Marks a message whose dialect definition declares target_system /
/// target_component payload fields — a message that CAN be addressed to a
/// specific system/component.
///
/// Adds ZERO members. The properties already exist on the DTO as ordinary
/// generated payload fields, and their declared shape deliberately differs
/// between messages: byte on COMMAND_LONG (core fields), byte? on COMMAND_ACK
/// (fields declared after &lt;extensions/&gt;). Keeping this marker empty is
/// exactly what lets the "nullable == extension field" convention hold across
/// the whole dialect with no exceptions. All stamping BEHAVIOUR lives in the
/// companion — see <see cref="IMavlinkTargetedMessageInfo{T}"/>.
///
/// Purpose: the compile-time constraint on MavlinkClient.SendToAsync and
/// MavlinkPeer.SendAsync — you can only address something that is physically
/// addressable.
///
/// Implementing THIS ALONE means addressing is OPTIONAL, and an unaddressed
/// send through plain SendAsync is legitimate protocol usage — PING with
/// target 0/0 is a ping request to everyone, and a v1 COMMAND_ACK carries no
/// target bytes on the wire at all. Messages where an unaddressed send is
/// almost certainly a mistake implement
/// <see cref="IMavlinkTargetRequiredMessage"/> instead.
/// </summary>
public interface IMavlinkTargetedMessage : IMavlinkMessage { }
