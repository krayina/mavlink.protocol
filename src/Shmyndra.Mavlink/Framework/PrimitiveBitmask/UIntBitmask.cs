namespace Mavlink.Bitmask;

public readonly struct UIntBitmask : IEquatable<UIntBitmask>
{
	public const int Width = 32;

	private readonly uint _bits;

	public UIntBitmask(uint bits) => _bits = bits;

	public uint Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool IsBitSet(int index) =>
		(uint)index < Width && (_bits & (((uint)1) << index)) != 0;

	public UIntBitmask WithBit(int index, bool value)
	{
		if ((uint)index >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		return new UIntBitmask(value
			? (uint)(_bits | (((uint)1) << index))
			: (uint)(_bits & ~(((uint)1) << index)));
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

	public static explicit operator uint(UIntBitmask value) => value._bits;
	public static explicit operator UIntBitmask(uint value) => new(value);

	public static UIntBitmask operator |(UIntBitmask l, UIntBitmask r) => new((uint)(l._bits | r._bits));
	public static UIntBitmask operator &(UIntBitmask l, UIntBitmask r) => new((uint)(l._bits & r._bits));
	public static UIntBitmask operator ~(UIntBitmask v) => new((uint)~v._bits);

	public bool Equals(UIntBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is UIntBitmask other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(UIntBitmask l, UIntBitmask r) => l._bits == r._bits;
	public static bool operator !=(UIntBitmask l, UIntBitmask r) => l._bits != r._bits;

	public override string ToString() => "0b" + Convert.ToString((long)_bits, 2).PadLeft(Width, '0');
}
