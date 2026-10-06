using System.Diagnostics.CodeAnalysis;

namespace Mavlink;

/// <summary>
/// Zero-allocation enumeration of set flags, O(popcount).
/// The caller passes <c>bits &amp; DefinedMask</c>, so every yielded value is a defined single-bit flag.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
	Justification = "Mutable iteration cursor; comparing enumerators is meaningless.")]
public struct FlagEnumerator<TEnum> where TEnum : struct, Enum
{
	private ulong _remaining;

	public FlagEnumerator(ulong definedSetBits)
	{
		_remaining = definedSetBits;
		Current = default;
	}

	public TEnum Current { get; private set; }

	public bool MoveNext()
	{
		if (_remaining == 0)
		{
			return false;
		}
		Current = EnumBits.FromBits<TEnum>(1UL << BitHelper.TrailingZeroCount(_remaining));
		_remaining &= _remaining - 1;
		return true;
	}

	public FlagEnumerator<TEnum> GetEnumerator() => this;
}
