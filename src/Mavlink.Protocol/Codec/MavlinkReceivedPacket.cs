#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mavlink;

/// <summary>
/// A received, CRC-checked frame: its header fields and an inline copy of its payload.
/// </summary>
/// <remarks>
/// The payload lives inside the struct (no heap allocation, no ownership to track), so
/// packets can be queued and dispatched by value. Bytes past <see cref="PayloadLength"/>
/// are always zero, which makes undoing MAVLink 2 truncation free: a longer view of the
/// same buffer is the restored payload.
/// <para>
/// Payload access by target: <c>Payload</c> (a span into this packet, its lifetime
/// enforced by the compiler) on netstandard2.1 and every .NET Core TFM;
/// <see cref="CopyPayloadTo(Span{byte})"/> everywhere. netstandard2.0's portable span
/// cannot be created over a managed reference, and a pointer-based one would dangle as
/// soon as the GC moved the packet (it lives in channel queues and async state machines),
/// so that target only offers the copy.
/// </para>
/// </remarks>
public readonly record struct MavlinkReceivedPacket
{
	public uint MessageId { get; }
	public byte SenderSystemId { get; }
	public byte SenderComponentId { get; }
	public byte Sequence { get; }
	public MavlinkPacketVersion Version { get; }
	public bool IsSigned { get; }

	/// <summary>
	/// Payload length as received (the LEN header byte).
	/// </summary>
	public byte PayloadLength { get; }

	private readonly MavlinkPayloadBuffer _payload;

	public MavlinkReceivedPacket(
		uint messageId,
		byte sysId,
		byte compId,
		byte seq,
		MavlinkPacketVersion version,
		bool isSigned,
		ReadOnlySpan<byte> payloadSpan)
	{
		if (payloadSpan.Length > MavlinkConstants.MAX_PAYLOAD_LENGTH)
		{
			throw new ArgumentException("Payload exceeds the MAVLink maximum of 255 bytes.", nameof(payloadSpan));
		}

		MessageId = messageId;
		SenderSystemId = sysId;
		SenderComponentId = compId;
		Sequence = seq;
		Version = version;
		IsSigned = isSigned;
		PayloadLength = (byte)payloadSpan.Length;

		// Zero-fill is load-bearing: bytes past PayloadLength must stay zero (see remarks).
		_payload = default;

		if (!payloadSpan.IsEmpty)
		{
			Unsafe.CopyBlockUnaligned(
				ref _payload.FirstByte(),
				ref MemoryMarshal.GetReference(payloadSpan),
				(uint)payloadSpan.Length);
		}
	}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
	/// <summary>
	/// Payload bytes as received (MAVLink 2 payloads arrive with trailing zeros stripped).
	/// The span points into this packet; the compiler prevents it from outliving it.
	/// </summary>
	[UnscopedRef]
	public ReadOnlySpan<byte> Payload => Buffer.Slice(0, PayloadLength);

	/// <summary>
	/// Payload view at least <paramref name="minLength"/> bytes long: bytes past
	/// <see cref="PayloadLength"/> are the zeros MAVLink 2 truncation removed.
	/// </summary>
	[UnscopedRef]
	internal ReadOnlySpan<byte> GetPayload(int minLength)
		=> Buffer.Slice(0, Math.Max(PayloadLength, minLength));

	[UnscopedRef]
	private ReadOnlySpan<byte> Buffer
#if NET8_0_OR_GREATER
		=> _payload;
#else
		=> MemoryMarshal.CreateReadOnlySpan(ref _payload.FirstByte(), MavlinkConstants.MAX_PAYLOAD_LENGTH);
#endif
#endif

	/// <summary>
	/// Copies the payload as received into <paramref name="destination"/> and returns the
	/// number of bytes written (<see cref="PayloadLength"/>). Available on every target.
	/// </summary>
	public int CopyPayloadTo(Span<byte> destination) => CopyPayloadTo(destination, minLength: 0);

	/// <summary>
	/// Copies the payload zero-extended to at least <paramref name="minLength"/> bytes.
	/// </summary>
	internal int CopyPayloadTo(Span<byte> destination, int minLength)
	{
		if ((uint)minLength > MavlinkConstants.MAX_PAYLOAD_LENGTH)
		{
			throw new ArgumentOutOfRangeException(nameof(minLength));
		}

		int length = Math.Max(PayloadLength, minLength);
		if (destination.Length < length)
		{
			throw CodecThrowHelper.DestinationTooShort(length, destination.Length);
		}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
		Buffer.Slice(0, length).CopyTo(destination);
#else
		Unsafe.CopyBlockUnaligned(
			ref MemoryMarshal.GetReference(destination),
			ref _payload.FirstByte(),
			(uint)length);
#endif
		return length;
	}

	// The synthesized record equality would run ValueType.Equals over the buffer: boxing
	// on every call, and on netstandard2.0 (an explicit-size struct is not "tightly packed")
	// a field-by-field compare that looks at the first payload byte only.
	public bool Equals(MavlinkReceivedPacket other)
	{
		if (MessageId != other.MessageId
			|| SenderSystemId != other.SenderSystemId
			|| SenderComponentId != other.SenderComponentId
			|| Sequence != other.Sequence
			|| Version != other.Version
			|| IsSigned != other.IsSigned
			|| PayloadLength != other.PayloadLength)
		{
			return false;
		}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
		return Payload.SequenceEqual(other.Payload);
#else
		ref byte left = ref _payload.FirstByte();
		ref byte right = ref other._payload.FirstByte();
		for (int i = 0; i < PayloadLength; i++)
		{
			if (Unsafe.Add(ref left, i) != Unsafe.Add(ref right, i))
			{
				return false;
			}
		}
		return true;
#endif
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = (int)MessageId;
			hash = (hash * 31) + ((SenderSystemId << 8) | SenderComponentId);
			hash = (hash * 31) + Sequence;
			hash = (hash * 31) + (((int)Version << 1) | (IsSigned ? 1 : 0));
			hash = (hash * 31) + PayloadLength;

			ref byte payload = ref _payload.FirstByte();
			for (int i = 0; i < PayloadLength; i++)
			{
				hash = (hash * 31) + Unsafe.Add(ref payload, i);
			}
			return hash;
		}
	}
}
