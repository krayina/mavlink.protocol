using System.Runtime.CompilerServices;

namespace Mavlink;

/// <summary>
/// Reinterpretation between an enum and its raw bits without boxing.
/// <see cref="Unsafe.SizeOf{T}"/> folds to a constant per instantiation, so the
/// switch is eliminated by the JIT and by ILC.
/// </summary>
internal static class EnumBits
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ToBits<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		switch (Unsafe.SizeOf<TEnum>())
		{
			case 1: return Unsafe.As<TEnum, byte>(ref value);
			case 2: return Unsafe.As<TEnum, ushort>(ref value);
			case 4: return Unsafe.As<TEnum, uint>(ref value);
			default: return Unsafe.As<TEnum, ulong>(ref value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TEnum FromBits<TEnum>(ulong bits) where TEnum : struct, Enum
	{
		switch (Unsafe.SizeOf<TEnum>())
		{
			case 1: { byte v = (byte)bits; return Unsafe.As<byte, TEnum>(ref v); }
			case 2: { ushort v = (ushort)bits; return Unsafe.As<ushort, TEnum>(ref v); }
			case 4: { uint v = (uint)bits; return Unsafe.As<uint, TEnum>(ref v); }
			default: return Unsafe.As<ulong, TEnum>(ref bits);
		}
	}
}
