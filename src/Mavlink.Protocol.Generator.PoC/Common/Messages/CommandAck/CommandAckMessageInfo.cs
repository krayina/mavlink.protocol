namespace Mavlink.Common.Codecs.Metadata;

public sealed class CommandAckMessageInfo : IMavlinkTargetedMessageInfo<CommandAckMavlinkMessage>
{
	public static readonly CommandAckMessageInfo Instance = new CommandAckMessageInfo();

	public uint MessageId => 77;
	public byte CrcExtra => 143;
	public string Name => "COMMAND_ACK";
	public Type MessageType => typeof(CommandAckMavlinkMessage);

	public int PayloadLength => 3;
	public int PayloadLengthWithExtensions => 10;
	public bool HasExtensions => true;

	/// <summary>
	/// target_system/target_component are declared after &lt;extensions/&gt;,
	/// so a v1 frame ends after result and cannot carry an address at all.
	/// SendToAsync throws on v1 for this message; plain SendAsync stays the
	/// correct, spec-conformant way to emit an unaddressed v1 COMMAND_ACK.
	/// </summary>
	public bool TargetFieldsAreExtensions => true;

	public IMavlinkPayloadSerializer<CommandAckMavlinkMessage> PayloadSerializer { get; }
		= new Payload.CommandAckPayloadSerializer();

	public int SerializePayloadV1(IMavlinkMessage message, Span<byte> destination)
	{
		if (message is CommandAckMavlinkMessage msg)
		{
			return PayloadSerializer.SerializeV1(msg, destination);
		}
		throw new ArgumentException($"Incorrect message type. Expected {nameof(CommandAckMavlinkMessage)}, got {message.GetType().Name}");
	}

	public int SerializePayloadV2(IMavlinkMessage message, Span<byte> destination)
	{
		if (message is CommandAckMavlinkMessage msg)
		{
			return PayloadSerializer.SerializeV2(msg, destination);
		}
		throw new ArgumentException($"Incorrect message type. Expected {nameof(CommandAckMavlinkMessage)}, got {message.GetType().Name}");
	}

	public IMavlinkMessage DeserializePayloadV1(ReadOnlySpan<byte> payload)
	{
		return PayloadSerializer.DeserializeV1(payload);
	}

	public IMavlinkMessage DeserializePayloadV2(ReadOnlySpan<byte> payload)
	{
		return PayloadSerializer.DeserializeV2(payload);
	}

	public CommandAckMavlinkMessage WithTarget(
		in CommandAckMavlinkMessage message, byte targetSystem, byte targetComponent)
	{
		// byte → byte? implicitly; the nullable shape of the extension fields
		// never has to satisfy an interface member, so the DTO stays untouched.
		return message with { TargetSystem = targetSystem, TargetComponent = targetComponent };
	}

	public IMavlinkMessage WithTarget(
		IMavlinkMessage message, byte targetSystem, byte targetComponent)
	{
		if (message is CommandAckMavlinkMessage msg)
		{
			return msg with { TargetSystem = targetSystem, TargetComponent = targetComponent };
		}
		throw new ArgumentException($"Incorrect message type. Expected {nameof(CommandAckMavlinkMessage)}, got {message.GetType().Name}");
	}
}
