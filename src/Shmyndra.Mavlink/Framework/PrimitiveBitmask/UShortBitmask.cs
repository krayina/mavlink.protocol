namespace Mavlink.Bitmask;

public readonly struct UShortBitmask : IEquatable<UShortBitmask>
{
	public const int Width = 16;

	private readonly ushort _bits;

	public UShortBitmask(ushort bits) => _bits = bits;

	public ushort Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (((ushort)1) << index)) != 0;

	public UShortBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new UShortBitmask(value
			? (ushort)(_bits | (((ushort)1) << index))
			: (ushort)(_bits & ~(((ushort)1) << index)));
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

	public static explicit operator ushort(UShortBitmask value) => value._bits;
	public static explicit operator UShortBitmask(ushort value) => new(value);

	public static UShortBitmask operator |(UShortBitmask l, UShortBitmask r) => new((ushort)(l._bits | r._bits));
	public static UShortBitmask operator &(UShortBitmask l, UShortBitmask r) => new((ushort)(l._bits & r._bits));
	public static UShortBitmask operator ~(UShortBitmask v) => new((ushort)~v._bits);

	public bool Equals(UShortBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is UShortBitmask other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(UShortBitmask l, UShortBitmask r) => l._bits == r._bits;
	public static bool operator !=(UShortBitmask l, UShortBitmask r) => l._bits != r._bits;

	public override string ToString() => "0b" + Convert.ToString((long)_bits, 2).PadLeft(Width, '0');
}
