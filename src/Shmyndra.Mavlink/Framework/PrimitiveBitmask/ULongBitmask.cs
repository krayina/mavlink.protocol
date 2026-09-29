namespace Mavlink.Bitmask;

public readonly struct ULongBitmask : IEquatable<ULongBitmask>
{
	public const int Width = 64;

	private readonly ulong _bits;

	public ULongBitmask(ulong bits) => _bits = bits;

	public ulong Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (((ulong)1) << index)) != 0;

	public ULongBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new ULongBitmask(value
			? (ulong)(_bits | (((ulong)1) << index))
			: (ulong)(_bits & ~(((ulong)1) << index)));
	}

	public BitIndexEnumerator GetEnumerator() => new(_bits);

	public int[] ToIndexArray()
	{
		if (_bits == 0)
		{
			return Array.Empty<int>();
		}
		var result = new int[Count];
		int i = 0;
		foreach (var index in this)
		{
			result[i++] = index;
		}
		return result;
	}

	public int WriteIndicesTo(Span<int> destination)
	{
		int i = 0;
		foreach (var index in this)
		{
			if (i >= destination.Length)
			{
				throw new ArgumentException("Buffer too small.", nameof(destination));
			}
			destination[i++] = index;
		}
		return i;
	}

	public static explicit operator ulong(ULongBitmask value) => value._bits;
	public static explicit operator ULongBitmask(ulong value) => new(value);

	public static ULongBitmask operator |(ULongBitmask l, ULongBitmask r) => new((ulong)(l._bits | r._bits));
	public static ULongBitmask operator &(ULongBitmask l, ULongBitmask r) => new((ulong)(l._bits & r._bits));
	public static ULongBitmask operator ~(ULongBitmask v) => new((ulong)~v._bits);

	public bool Equals(ULongBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is ULongBitmask other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(ULongBitmask l, ULongBitmask r) => l._bits == r._bits;
	public static bool operator !=(ULongBitmask l, ULongBitmask r) => l._bits != r._bits;

	public override string ToString() => "0b" + Convert.ToString((long)_bits, 2).PadLeft(Width, '0');
}
