namespace Mavlink.Bitmask;

/// <summary>
/// 64-bit bitmask for <c>display="bitmask"</c> fields that have no enum.
/// </summary>
public readonly struct ULongBitmask : IEquatable<ULongBitmask>
#if NET6_0_OR_GREATER
	, ISpanFormattable
#else
	, IFormattable
#endif
{
	public const int Width = 64;

	private readonly ulong _bits;

	public ULongBitmask(ulong bits) => _bits = bits;

	public static ULongBitmask Empty => default;

	public ulong Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (1UL << index)) != 0;

	public ULongBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new ULongBitmask(value
			? _bits | (1UL << index)
			: _bits & ~(1UL << index));
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

	public static explicit operator ulong(ULongBitmask value) => value._bits;
	public static explicit operator ULongBitmask(ulong value) => new(value);

	public static ULongBitmask operator |(ULongBitmask l, ULongBitmask r) => new(l._bits | r._bits);
	public static ULongBitmask operator &(ULongBitmask l, ULongBitmask r) => new(l._bits & r._bits);
	public static ULongBitmask operator ^(ULongBitmask l, ULongBitmask r) => new(l._bits ^ r._bits);
	public static ULongBitmask operator ~(ULongBitmask v) => new(~v._bits);

	public bool Equals(ULongBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is ULongBitmask other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(ULongBitmask l, ULongBitmask r) => l._bits == r._bits;
	public static bool operator !=(ULongBitmask l, ULongBitmask r) => l._bits != r._bits;

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
