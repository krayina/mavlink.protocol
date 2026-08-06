namespace Mavlink;

/// <summary>
/// Marks a targeted message that should never be sent unaddressed. Leaving
/// target_system at 0 is legal on the wire — 0 IS broadcast per the MAVLink
/// spec — but for these messages it is almost always a forgotten line rather
/// than an intent: a command meant for one vehicle would be executed by every
/// vehicle on the link.
///
/// This is a LIBRARY POLICY, not a fact derivable from the dialect XML. The
/// schema has no machine-readable way to express "broadcast is meaningful
/// here"; that information exists only in the human-readable
/// &lt;description&gt;. The generator assigns this marker from a curated
/// policy file: core target fields imply required, minus documented
/// exceptions (PING, discovery-style requests).
///
/// Adds ZERO members, and nothing at runtime branches on it — the send path is
/// byte-for-byte identical. Its only consumer is the MAV001 analyzer, which
/// flags SendAsync calls whose argument implements this interface and points
/// the user at SendToAsync / To(...).
/// </summary>
public interface IMavlinkTargetRequiredMessage : IMavlinkTargetedMessage { }
