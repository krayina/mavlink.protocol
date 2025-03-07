using System.Collections.Immutable;

namespace PrimitiveBitmask;

public interface IPrimitiveBitmask<T> where T : struct
#if NET8_0_OR_GREATER
	, System.Numerics.IUnsignedNumber<T>, System.Numerics.IBinaryInteger<T>
#endif
{
	/// <summary>
	/// Returns the raw value of the bitmask.
	/// </summary>
	T Bitmask { get; }

	/// <summary>
	/// Returns an immutable array of indices where bits are set to true.
	/// </summary>
	ImmutableArray<int> TrueIndices { get; }

	/// <summary>
	/// Checks whether the bit at the specified index is set.
	/// </summary>
	bool IsBitSet(int index);
}
