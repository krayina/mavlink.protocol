namespace Mavlink.Bitmask;

/// <summary>
/// 32-bit bitmask for <c>display="bitmask"</c> fields that have no enum.
/// </summary>
public readonly struct UIntBitmask : IEquatable<UIntBitmask>
#if NET6_0_OR_GREATER
	, ISpanFormattable
#else
	, IFormattable
#endif
{
	public const int Width = 32;

	private readonly uint _bits;

	public UIntBitmask(uint bits) => _bits = bits;

	public static UIntBitmask Empty => default;

	public uint Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (1u << index)) != 0;

	public UIntBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new UIntBitmask(value
			? _bits | (1u << index)
			: _bits & ~(1u << index));
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

	public static explicit operator uint(UIntBitmask value) => value._bits;
	public static explicit operator UIntBitmask(uint value) => new(value);

	public static implicit operator ULongBitmask(UIntBitmask value) => new(value._bits);

	public static UIntBitmask operator |(UIntBitmask l, UIntBitmask r) => new(l._bits | r._bits);
	public static UIntBitmask operator &(UIntBitmask l, UIntBitmask r) => new(l._bits & r._bits);
	public static UIntBitmask operator ^(UIntBitmask l, UIntBitmask r) => new(l._bits ^ r._bits);
	public static UIntBitmask operator ~(UIntBitmask v) => new(~v._bits);

	public bool Equals(UIntBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is UIntBitmask other && Equals(other);
	public override int GetHashCode() => (int)_bits;
	public static bool operator ==(UIntBitmask l, UIntBitmask r) => l._bits == r._bits;
	public static bool operator !=(UIntBitmask l, UIntBitmask r) => l._bits != r._bits;

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
