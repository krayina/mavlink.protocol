using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif

namespace Mavlink.Bitmask;

internal static class BitHelper
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int PopCount(ulong v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.PopCount(v);
#else
		v -= (v >> 1) & 0x5555555555555555UL;
		v = (v & 0x3333333333333333UL) + ((v >> 2) & 0x3333333333333333UL);
		v = (v + (v >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
		return (int)((v * 0x0101010101010101UL) >> 56);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int PopCount(uint v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.PopCount(v);
#else
		v -= (v >> 1) & 0x55555555u;
		v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
		v = (v + (v >> 4)) & 0x0F0F0F0Fu;
		return (int)((v * 0x01010101u) >> 24);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int TrailingZeroCount(ulong v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.TrailingZeroCount(v);
#else
		if (v == 0)
		{
			return 64;
		}
		// Stay in the unsigned domain: ~v + 1 never overflows for v != 0.
		return PopCount(unchecked((v & (~v + 1UL)) - 1UL));
#endif
	}
}
