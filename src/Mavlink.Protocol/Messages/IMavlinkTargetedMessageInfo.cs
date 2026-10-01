namespace Mavlink;

/// <summary>
/// Non-generic targeting metadata for boxed flows (queues, relaying,
/// SubscribeAll round-trips) — mirrors the generic/non-generic duality of
/// IMavlinkMessageInfo. Boxed input, boxed output: these paths are already
/// boxed by design, so nothing gets worse.
/// </summary>
public interface IMavlinkTargetedMessageInfo : IMavlinkMessageInfo
{
	IMavlinkMessage WithTarget(IMavlinkMessage message, byte targetSystem, byte targetComponent);

	/// <summary>
	/// True when target_system/target_component are declared AFTER
	/// &lt;extensions/&gt; (COMMAND_ACK is the case in common.xml).
	///
	/// A v1 frame stops at the end of the core payload, so those bytes are
	/// simply not on the wire — there is no field to fill in and nothing to
	/// misread. That is exactly why SendToAsync throws instead of proceeding:
	/// the caller asked for the message to be addressed, and the frame it
	/// chose cannot carry an address, so the send would quietly do something
	/// other than what was requested.
	///
	/// Sending such a message UNADDRESSED over v1 is legitimate and stays
	/// available through plain SendAsync — that is how v1 stacks have always
	/// emitted COMMAND_ACK, and the marker on the DTO is
	/// IMavlinkTargetedMessage (optional targeting), never
	/// IMavlinkTargetRequiredMessage.
	///
	/// Note this is the opposite case from the "0 means broadcast" hazard,
	/// which applies to CORE target fields — i.e. when this property is false.
	/// </summary>
	bool TargetFieldsAreExtensions { get; }
}

/// <summary>
/// Generic targeting metadata: the generated *MessageInfo companion of a
/// targeted message implements this instead of IMavlinkMessageInfo&lt;T&gt;,
/// which it already extends — so the two cannot drift onto different T.
///
/// This is where the stamping behaviour lives INSTEAD of the DTO — the same
/// architectural decision already proven by keeping serialization in the
/// companion rather than on the message. The generated body is a single
/// `with` expression: legal from external code because record-struct init
/// accessors are public, so the DTO needs no extra members. That emptiness is
/// what lets IMavlinkTargetedMessage stay a pure marker, and therefore what
/// lets the "nullable == extension field" convention hold with no exceptions:
/// TargetSystem is `byte` on COMMAND_LONG and `byte?` on COMMAND_ACK, and
/// neither has to satisfy an interface member.
///
/// Cost: the companion is a class, so this is an ordinary interface call; the
/// MESSAGE is what stays off the heap. It is passed by `in` (no copy at the
/// call, no defensive copy — the DTO is a readonly record struct), and the
/// single copy is the `with` result being returned.
/// </summary>
public interface IMavlinkTargetedMessageInfo<T>
	: IMavlinkTargetedMessageInfo, IMavlinkMessageInfo<T>
	where T : struct, IMavlinkTargetedMessage
{
	/// <summary>Returns a copy of the message with the target fields set.
	/// Always OVERWRITES existing values: in SendToAsync the target argument
	/// wins over whatever the user pre-set in the struct.</summary>
	T WithTarget(in T message, byte targetSystem, byte targetComponent);
}
