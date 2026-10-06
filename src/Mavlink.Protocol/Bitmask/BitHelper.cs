using System.ComponentModel;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif

namespace Mavlink;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class BitHelper
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int PopCount(ulong v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.PopCount(v);
#else
		return PopCountSoftware(v);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int PopCount(uint v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.PopCount(v);
#else
		return PopCountSoftware(v);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int TrailingZeroCount(ulong v)
	{
#if NETCOREAPP3_0_OR_GREATER
		return BitOperations.TrailingZeroCount(v);
#else
		return TrailingZeroCountSoftware(v);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int PopCountSoftware(ulong v)
	{
		v -= (v >> 1) & 0x5555555555555555UL;
		v = (v & 0x3333333333333333UL) + ((v >> 2) & 0x3333333333333333UL);
		v = (v + (v >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
		return (int)(unchecked(v * 0x0101010101010101UL) >> 56);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int PopCountSoftware(uint v)
	{
		v -= (v >> 1) & 0x55555555u;
		v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
		v = (v + (v >> 4)) & 0x0F0F0F0Fu;
		return (int)(unchecked(v * 0x01010101u) >> 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int TrailingZeroCountSoftware(ulong v)
	{
		if (v == 0)
		{
			return 64;
		}
		// Isolate the lowest set bit. Unsigned negation must be unchecked so a consumer's
		// CheckForOverflowUnderflow setting cannot turn it into an OverflowException.
		ulong lowest = unchecked(v & (0UL - v));
		return DeBruijnIndex64[(int)(unchecked(lowest * DeBruijn64) >> 58)];
	}

	private const ulong DeBruijn64 = 0x03F79D71B4CB0A89UL;

	private static ReadOnlySpan<byte> DeBruijnIndex64 => new byte[]
	{
		 0,  1, 48,  2, 57, 49, 28,  3,
		61, 58, 50, 42, 38, 29, 17,  4,
		62, 55, 59, 36, 53, 51, 43, 22,
		45, 39, 33, 30, 24, 18, 12,  5,
		63, 47, 56, 27, 60, 41, 37, 16,
		54, 35, 52, 21, 44, 32, 23, 11,
		46, 26, 40, 15, 34, 20, 31, 10,
		25, 14, 19,  9, 13,  8,  7,  6,
	};
}
