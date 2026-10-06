namespace Mavlink;

/// <summary>
/// 16-bit bitmask for <c>display="bitmask"</c> fields that have no enum.
/// </summary>
public readonly struct UShortBitmask : IEquatable<UShortBitmask>
#if NET6_0_OR_GREATER
	, ISpanFormattable
#else
	, IFormattable
#endif
{
	public const int Width = 16;

	private readonly ushort _bits;

	public UShortBitmask(ushort bits) => _bits = bits;

	public static UShortBitmask Empty => default;

	public ushort Bits => _bits;
	public int Count => BitHelper.PopCount((uint)_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (1u << index)) != 0;

	public UShortBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new UShortBitmask(value
			? (ushort)(_bits | (1u << index))
			: (ushort)(_bits & ~(1u << index)));
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

	public static explicit operator ushort(UShortBitmask value) => value._bits;
	public static explicit operator UShortBitmask(ushort value) => new(value);

	public static implicit operator UIntBitmask(UShortBitmask value) => new(value._bits);
	public static implicit operator ULongBitmask(UShortBitmask value) => new(value._bits);

	public static UShortBitmask operator |(UShortBitmask l, UShortBitmask r) => new((ushort)(l._bits | r._bits));
	public static UShortBitmask operator &(UShortBitmask l, UShortBitmask r) => new((ushort)(l._bits & r._bits));
	public static UShortBitmask operator ^(UShortBitmask l, UShortBitmask r) => new((ushort)(l._bits ^ r._bits));
	public static UShortBitmask operator ~(UShortBitmask v) => new((ushort)~v._bits);

	public bool Equals(UShortBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is UShortBitmask other && Equals(other);
	public override int GetHashCode() => _bits;
	public static bool operator ==(UShortBitmask l, UShortBitmask r) => l._bits == r._bits;
	public static bool operator !=(UShortBitmask l, UShortBitmask r) => l._bits != r._bits;

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
