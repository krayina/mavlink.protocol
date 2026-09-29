namespace Mavlink.Bitmask;

public readonly struct ByteBitmask : IEquatable<ByteBitmask>
{
	public const int Width = 8;

	private readonly byte _bits;

	public ByteBitmask(byte bits) => _bits = bits;

	public byte Bits => _bits;
	public int Count => BitHelper.PopCount(_bits);
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

	public static explicit operator byte(ByteBitmask value) => value._bits;
	public static explicit operator ByteBitmask(byte value) => new(value);

	public static ByteBitmask operator |(ByteBitmask l, ByteBitmask r) => new((byte)(l._bits | r._bits));
	public static ByteBitmask operator &(ByteBitmask l, ByteBitmask r) => new((byte)(l._bits & r._bits));
	public static ByteBitmask operator ~(ByteBitmask v) => new((byte)~v._bits);

	public bool Equals(ByteBitmask other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is ByteBitmask other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(ByteBitmask l, ByteBitmask r) => l._bits == r._bits;
	public static bool operator !=(ByteBitmask l, ByteBitmask r) => l._bits != r._bits;

	public override string ToString() => "0b" + Convert.ToString(_bits, 2).PadLeft(Width, '0');
}
