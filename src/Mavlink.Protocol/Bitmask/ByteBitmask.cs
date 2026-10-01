namespace Mavlink.Bitmask;

/// <summary>
/// 8-bit bitmask for <c>display="bitmask"</c> fields that have no enum.
/// </summary>
public readonly struct ByteBitmask : IEquatable<ByteBitmask>
#if NET6_0_OR_GREATER
	, ISpanFormattable
#else
	, IFormattable
#endif
{
	public const int Width = 8;

	private readonly byte _bits;

	public ByteBitmask(byte bits) => _bits = bits;

	public static ByteBitmask Empty => default;

	public byte Bits => _bits;
	public int Count => BitHelper.PopCount((uint)_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (1u << index)) != 0;

	public ByteBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new ByteBitmask(value
			? (byte)(_bits | (1u << index))
			: (byte)(_bits & ~(1u << index)));
	}

	public BitIndexEnumerator GetEnumerator() => new(_bits);

	public int[] ToIndexArray()
	{
		int count = Count;
		if (count == 0)
		{
			return Array.Empty<int>();
		}
		var result = new int[count];
		int i = 0;
		foreach (int index in this)
		{
			result[i++] = index;
		}
		return result;
	}

	public int WriteIndicesTo(Span<int> destination)
	{
		int count = Count;
		if (destination.Length < count)
		{
			throw new ArgumentException("Buffer too small.", nameof(destination));
		}
		int i = 0;
		foreach (int index in this)
		{
			destination[i++] = index;
		}
		return i;
	}

	public BitmaskDisplay ToDisplay() => BitmaskDisplay.Unnamed(_bits, Width);

	public static explicit operator byte(ByteBitmask value) => value._bits;
	public static explicit operator ByteBitmask(byte value) => new(value);

	public static implicit operator UShortBitmask(ByteBitmask value) => new(value._bits);
	public static implicit operator UIntBitmask(ByteBitmask value) => new(value._bits);
	public static implicit operator ULongBitmask(ByteBitmask value) => new(value._bits);

	public static ByteBitmask operator |(ByteBitmask l, ByteBitmask r) => new((byte)(l._bits | r._bits));
	public static ByteBitmask operator &(ByteBitmask l, ByteBitmask r) => new((byte)(l._bits & r._bits));
	public static ByteBitmask operator ^(ByteBitmask l, ByteBitmask r) => new((byte)(l._bits ^ r._bits));
	public static ByteBitmask operator ~(ByteBitmask v) => new((byte)~v._bits);

	public bool Equals(ByteBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is ByteBitmask other && Equals(other);
	public override int GetHashCode() => _bits;
	public static bool operator ==(ByteBitmask l, ByteBitmask r) => l._bits == r._bits;
	public static bool operator !=(ByteBitmask l, ByteBitmask r) => l._bits != r._bits;

	public override string ToString() => ToDisplay().ToString("B", null);

	public string ToString(string? format, IFormatProvider? provider) =>
		ToDisplay().ToString(string.IsNullOrEmpty(format) ? "B" : format, provider);

#if NET6_0_OR_GREATER
	public bool TryFormat(Span<char> destination, out int charsWritten,
		ReadOnlySpan<char> format, IFormatProvider? provider) =>
		ToDisplay().TryFormat(destination, out charsWritten,
			format.IsEmpty ? "B".AsSpan() : format, provider);
#endif
}
