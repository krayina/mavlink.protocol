using System.Diagnostics.CodeAnalysis;

namespace Mavlink;

/// <summary>
/// Zero-allocation enumeration of set bit indices. O(popcount).
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
Justification = "Mutable iteration cursor; comparing enumerators is meaningless. " +
				"BCL enumerators such as List<T>.Enumerator and Span<T>.Enumerator do the same.")]
public struct BitIndexEnumerator
{
	private ulong _remaining;
	public BitIndexEnumerator(ulong bits)
	{
		_remaining = bits;
		Current = -1;
	}

	public int Current { get; private set; }
	public bool MoveNext()
	{
		if (_remaining == 0) return false;
		Current = BitHelper.TrailingZeroCount(_remaining);
		_remaining &= _remaining - 1;
		return true;
	}
	public BitIndexEnumerator GetEnumerator() => this;
}
