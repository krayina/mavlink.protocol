using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mavlink.Bitmask;

public readonly struct EnumBitmask<TEnum> : IEquatable<EnumBitmask<TEnum>>
	where TEnum : struct, Enum
{
	private readonly ulong _bits;

	public EnumBitmask(TEnum value) => _bits = ToBits(value);
	private EnumBitmask(ulong bits) => _bits = bits;

	public static EnumBitmask<TEnum> FromRaw(ulong bits) => new(bits);

	public ulong Bits => _bits;
	public TEnum Value => FromBits(_bits);
	public int Count => BitHelper.PopCount(_bits);
	public bool IsEmpty => _bits == 0;

	public bool Has(TEnum flag)
	{
		var f = ToBits(flag);
		return f != 0 && (_bits & f) == f;
	}

	public bool HasAny(TEnum flags) => (_bits & ToBits(flags)) != 0;

	public ulong UnknownBits => _bits & ~Meta.DefinedMask;
	public bool HasUnknownBits => UnknownBits != 0;

	public EnumBitmask<TEnum> With(TEnum flag, bool value) =>
		new(value
			? _bits | ToBits(flag)
			: _bits & ~ToBits(flag)
		);

	public FlagEnumerator GetEnumerator() => new(_bits);

	[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
		Justification = "Mutable iteration cursor; comparing enumerators is meaningless. " +
						"BCL enumerators such as List<T>.Enumerator and Span<T>.Enumerator do the same.")]
	public struct FlagEnumerator
	{
		private readonly ulong _bits;
		private int _index;
		public FlagEnumerator(ulong bits) { _bits = bits; _index = -1; Current = default; }
		public TEnum Current { get; private set; }
		public bool MoveNext()
		{
			var flags = Meta.DefinedFlags;
			while (++_index < flags.Length)
			{
				var f = ToBits(flags[_index]);
				if ((_bits & f) != 0)
				{
					Current = flags[_index];
					return true;
				}
			}
			return false;
		}
		public FlagEnumerator GetEnumerator() => this;
	}

	public TEnum[] ToFlagArray()
	{
		if (_bits == 0)
		{
			return Array.Empty<TEnum>();
		}
		var buffer = new TEnum[Meta.DefinedFlags.Length];
		int i = 0;
		foreach (var flag in this)
		{
			buffer[i++] = flag;
		}
		if (i == buffer.Length)
		{
			return buffer;
		}
		var result = new TEnum[i];
		Array.Copy(buffer, result, i);
		return result;
	}

	public static implicit operator EnumBitmask<TEnum>(TEnum value) => new(value);
	public static EnumBitmask<TEnum> operator |(EnumBitmask<TEnum> l, EnumBitmask<TEnum> r) => new(l._bits | r._bits);
	public static EnumBitmask<TEnum> operator &(EnumBitmask<TEnum> l, EnumBitmask<TEnum> r) => new(l._bits & r._bits);

	public bool Equals(EnumBitmask<TEnum> other) => _bits == other._bits;
	public override bool Equals(object? obj) => obj is EnumBitmask<TEnum> other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode();
	public static bool operator ==(EnumBitmask<TEnum> l, EnumBitmask<TEnum> r) => l._bits == r._bits;
	public static bool operator !=(EnumBitmask<TEnum> l, EnumBitmask<TEnum> r) => l._bits != r._bits;

	public override string ToString()
	{
		if (_bits == 0)
		{
			return "(none)";
		}
		var parts = new List<string>();
		foreach (var flag in this)
		{
			parts.Add(flag.ToString());
		}
		if (HasUnknownBits)
		{
			parts.Add("unknown:0x" + UnknownBits.ToString("X", CultureInfo.InvariantCulture));
		}
		return string.Join(", ", parts);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong ToBits(TEnum value) => Unsafe.SizeOf<TEnum>() switch
	{
		1 => Unsafe.As<TEnum, byte>(ref value),
		2 => Unsafe.As<TEnum, ushort>(ref value),
		4 => Unsafe.As<TEnum, uint>(ref value),
		_ => Unsafe.As<TEnum, ulong>(ref value),
	};

	private static TEnum FromBits(ulong bits)
	{
		switch (Unsafe.SizeOf<TEnum>())
		{
			case 1: { byte v = (byte)bits; return Unsafe.As<byte, TEnum>(ref v); }
			case 2: { ushort v = (ushort)bits; return Unsafe.As<ushort, TEnum>(ref v); }
			case 4: { uint v = (uint)bits; return Unsafe.As<uint, TEnum>(ref v); }
			default: return Unsafe.As<ulong, TEnum>(ref bits);
		}
	}

	internal static class Meta
	{
		public static readonly TEnum[] DefinedFlags;
		public static readonly ulong DefinedMask;

		static Meta()
		{
			var values = (TEnum[])Enum.GetValues(typeof(TEnum));
			var singles = new List<TEnum>(values.Length);
			ulong mask = 0;
			foreach (var v in values)
			{
				var b = ToBits(v);
				if (b == 0 || (b & (b - 1)) != 0)
				{
					continue;
				}
				singles.Add(v);
				mask |= b;
			}
			DefinedFlags = singles.ToArray();
			DefinedMask = mask;
		}
	}
}
