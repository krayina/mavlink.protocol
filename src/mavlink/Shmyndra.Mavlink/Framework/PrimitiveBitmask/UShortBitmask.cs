using System.Collections.Immutable;

namespace PrimitiveBitmask;

public readonly struct UShortBitmask : IPrimitiveBitmask<ushort>
{
	private readonly ushort _bitmask;
	private readonly ImmutableArray<int> _trueIndices;

	public UShortBitmask(ushort bitmask)
	{
		_bitmask = bitmask;
		_trueIndices = default;
	}

	public ushort Bitmask => _bitmask;

	public ImmutableArray<int> TrueIndices => _trueIndices.IsDefault ? ComputeTrueIndices() : _trueIndices;

	public bool IsBitSet(int index)
	{
		return index >= 0 && index < 16 && (_bitmask & (1 << index)) != 0;
	}

#if NET8_0_OR_GREATER
	private ImmutableArray<int> ComputeTrueIndices()
	{
		int count = System.Numerics.BitOperations.PopCount(_bitmask);
		if (count == 0) return ImmutableArray<int>.Empty;

		int[] indices = new int[count];
		int currentIndex = 0;

		for (int i = 0; i < 16; i++)
		{
			if (IsBitSet(i))
			{
				indices[currentIndex] = i;
				currentIndex++;
			}
		}

		return indices.ToImmutableArray();
	}
#else
	private ImmutableArray<int> ComputeTrueIndices()
	{
		int[] indices = new int[16];
		int count = 0;

		for (int i = 0; i < 16; i++)
		{
			if (IsBitSet(i))
			{
				indices[count] = i;
				count++;
			}
		}

		return count == 0 ? ImmutableArray<int>.Empty : ImmutableArray.Create(indices, 0, count);
	}
#endif

	public override bool Equals(object? obj) => obj is UShortBitmask other && Equals(other);
	public bool Equals(UShortBitmask other) => _bitmask == other._bitmask;
	public override int GetHashCode() => _bitmask.GetHashCode();
	public static bool operator ==(UShortBitmask left, UShortBitmask right) => left.Equals(right);
	public static bool operator !=(UShortBitmask left, UShortBitmask right) => !left.Equals(right);

	public override string ToString() => $"Bitmask: {_bitmask} (True indices: {string.Join(", ", TrueIndices)})";
}
