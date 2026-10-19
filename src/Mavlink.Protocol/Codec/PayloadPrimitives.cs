using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mavlink;

/// <summary>
/// Little-endian access to MAVLink payload fields at fixed offsets, plus the two
/// length rules of the wire format (MAVLink 2 truncation and its reversal).
/// Generated payload serializers call only these members for byte-level work, so
/// every target-framework difference lives here and generated code is TFM-agnostic.
/// </summary>
/// <remarks>
/// Every read and write is bounds-checked. That is deliberate: the class is public,
/// and an unchecked accessor would make memory safety depend on the caller. Generated
/// code establishes the lengths once per call, so each check is an always-taken
/// branch; switching to unchecked access later would be a change to this class only.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PayloadPrimitives
{
	/// <summary>
	/// Largest payload either protocol version can carry (the LEN header byte).
	/// </summary>
	public const int MaxPayloadLength = MavlinkConstants.MAX_PAYLOAD_LENGTH;

	/// <summary>
	/// Positive quiet NaN (0x7FC00000): the bit pattern C's <c>NAN</c> and Python's
	/// <c>float('nan')</c> put on the wire. <see cref="float.NaN"/> is 0xFFC00000; any NaN
	/// is "invalid" to a receiver, but writing the canonical one keeps frames
	/// byte-identical to the reference implementations (golden tests, signing).
	/// </summary>
	public static float CanonicalNaNSingle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			int bits = 0x7FC00000;
			return Unsafe.As<int, float>(ref bits);
		}
	}

	/// <summary>
	/// Positive quiet NaN (0x7FF8000000000000); see <see cref="CanonicalNaNSingle"/>.
	/// </summary>
	public static double CanonicalNaNDouble
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			long bits = 0x7FF8000000000000;
			return Unsafe.As<long, double>(ref bits);
		}
	}

	// ------------------------------------------------------------------
	// Length rules
	// ------------------------------------------------------------------

	/// <summary>
	/// MAVLink 2 empty-byte truncation: the payload length without trailing zero
	/// bytes, but never below 1 — the first payload byte is always sent.
	/// This is a framing rule (it defines the LEN header byte), so the frame writer
	/// applies it; payload serializers always return the full length.
	/// </summary>
	public static int GetTruncatedLength(ReadOnlySpan<byte> payload)
	{
#if NET8_0_OR_GREATER
		int last = payload.LastIndexOfAnyExcept((byte)0);
		return last >= 0 ? last + 1 : Math.Min(payload.Length, 1);
#else
		int length = payload.Length;
		while (length > 1 && payload[length - 1] == 0)
		{
			length--;
		}
		return length;
#endif
	}

	/// <summary>
	/// Reverses truncation: copies <paramref name="payload"/> into the start of
	/// <paramref name="buffer"/>, zeroes the rest of it and returns <paramref name="buffer"/>.
	/// Generated deserializers call it only for a payload shorter than the definition,
	/// passing a <c>stackalloc</c> buffer of exactly the definition length.
	/// </summary>
	/// <remarks>
	/// The tail is cleared explicitly rather than relying on <c>stackalloc</c> zeroing:
	/// a consumer assembly compiled with <c>[module: SkipLocalsInit]</c> would otherwise
	/// hand generated code an uninitialized buffer.
	/// </remarks>
	public static ReadOnlySpan<byte> ZeroExtend(ReadOnlySpan<byte> payload, Span<byte> buffer)
	{
		if (payload.Length > buffer.Length)
		{
			throw CodecThrowHelper.DestinationTooShort(payload.Length, buffer.Length);
		}

		payload.CopyTo(buffer);
		buffer.Slice(payload.Length).Clear();
		return buffer;
	}

	// ------------------------------------------------------------------
	// Read
	// ------------------------------------------------------------------

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ReadUInt8(ReadOnlySpan<byte> source, int offset)
		=> source[offset];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static sbyte ReadInt8(ReadOnlySpan<byte> source, int offset)
		=> unchecked((sbyte)source[offset]); // must not throw under CheckForOverflowUnderflow

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort ReadUInt16(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ReadInt16(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ReadUInt32(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ReadUInt64(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64(ReadOnlySpan<byte> source, int offset)
		=> BinaryPrimitives.ReadInt64LittleEndian(source.Slice(offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReadSingle(ReadOnlySpan<byte> source, int offset)
	{
#if NET5_0_OR_GREATER
		return BinaryPrimitives.ReadSingleLittleEndian(source.Slice(offset));
#else
		// netstandard has no ReadSingleLittleEndian (and ns2.0 no Int32BitsToSingle).
		// Reinterpreting the bits keeps the exact pattern, NaN payloads included.
		int bits = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset));
		return Unsafe.As<int, float>(ref bits);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReadDouble(ReadOnlySpan<byte> source, int offset)
	{
#if NET5_0_OR_GREATER
		return BinaryPrimitives.ReadDoubleLittleEndian(source.Slice(offset));
#else
		long bits = BinaryPrimitives.ReadInt64LittleEndian(source.Slice(offset));
		return Unsafe.As<long, double>(ref bits);
#endif
	}

	// ------------------------------------------------------------------
	// Write
	// ------------------------------------------------------------------

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt8(Span<byte> destination, int offset, byte value)
		=> destination[offset] = value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt8(Span<byte> destination, int offset, sbyte value)
		=> destination[offset] = unchecked((byte)value); // must not throw under CheckForOverflowUnderflow

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt16(Span<byte> destination, int offset, ushort value)
		=> BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt16(Span<byte> destination, int offset, short value)
		=> BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt32(Span<byte> destination, int offset, uint value)
		=> BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32(Span<byte> destination, int offset, int value)
		=> BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt64(Span<byte> destination, int offset, ulong value)
		=> BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64(Span<byte> destination, int offset, long value)
		=> BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset), value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSingle(Span<byte> destination, int offset, float value)
	{
#if NET5_0_OR_GREATER
		BinaryPrimitives.WriteSingleLittleEndian(destination.Slice(offset), value);
#else
		BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), Unsafe.As<float, int>(ref value));
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteDouble(Span<byte> destination, int offset, double value)
	{
#if NET5_0_OR_GREATER
		BinaryPrimitives.WriteDoubleLittleEndian(destination.Slice(offset), value);
#else
		BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset), Unsafe.As<double, long>(ref value));
#endif
	}
}
