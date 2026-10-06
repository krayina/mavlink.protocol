using System.Buffers;
using System.Diagnostics;
#if NETCOREAPP2_1_OR_GREATER
using System.Globalization;
#endif

namespace Mavlink;

/// <summary>
/// Non-generic, allocation-free description of a bitmask value for UI, logging and diagnostics.
/// Obtained via <c>ToDisplay()</c> on primitive and generated enum bitmasks.
/// </summary>
/// <remarks>
/// Format specifiers: <c>"N"</c> (default) — C# names of set defined flags, or set bit
/// indices when no names are available; <c>"W"</c> — MAVLink wire names; <c>"B"</c> — binary.
/// Unknown bits are appended as <c>unknown:0x…</c> in name formats. The format provider is ignored.
/// </remarks>
public readonly struct BitmaskDisplay : IEquatable<BitmaskDisplay>
#if NET6_0_OR_GREATER
	, ISpanFormattable
#else
	, IFormattable
#endif
{
	private readonly ulong _bits;
	private readonly ulong _definedMask;
	private readonly BitmaskEntry[]? _entries;

	private BitmaskDisplay(ulong bits, int width, ulong definedMask, BitmaskEntry[]? entries)
	{
		_bits = bits;
		Width = width;
		_definedMask = definedMask;
		_entries = entries;
	}

	/// <summary>
	/// Describes a bitmask whose bits have no names. Name formats fall back to bit indices.
	/// </summary>
	/// <param name="width">Nominal width in bits, 1..64.</param>
	public static BitmaskDisplay Unnamed(ulong bits, int width)
	{
		ThrowIfInvalidWidth(width);
		return new BitmaskDisplay(bits, width, width == 64 ? ulong.MaxValue : (1UL << width) - 1, null);
	}

	/// <summary>
	/// Describes a bitmask backed by a generated enum definition.
	/// </summary>
	/// <param name="width">Nominal width in bits, 1..64.</param>
	/// <param name="definedMask">Union of all bits that have a definition.</param>
	/// <param name="entries">
	/// Exactly one entry per set bit of <paramref name="definedMask"/>, sorted by bit ascending.
	/// Not copied: the caller must never mutate it.
	/// </param>
	public static BitmaskDisplay Named(ulong bits, int width, ulong definedMask, BitmaskEntry[] entries)
	{
		ThrowIfInvalidWidth(width);
		if (entries is null)
		{
			throw new ArgumentNullException(nameof(entries));
		}
		Debug.Assert(entries.Length == BitHelper.PopCount(definedMask),
			"Entries must contain exactly one item per defined bit."
		);
		return new BitmaskDisplay(bits, width, definedMask, entries);
	}

	private static void ThrowIfInvalidWidth(int width)
	{
		if ((uint)(width - 1) >= 64)
		{
			throw new ArgumentOutOfRangeException(
				nameof(width), width, "Width must be in the range 1..64.");
		}
	}

	public ulong Bits => _bits;
	public int Width { get; }
	public ulong DefinedMask => _definedMask;
	public bool HasNames => _entries is not null;

	public int Count => BitHelper.PopCount(_bits & _definedMask);
	public ulong UnknownBits => _bits & ~_definedMask;
	public bool HasUnknownBits => UnknownBits != 0;

	/// <summary>
	/// All set bits, including unknown ones.
	/// </summary>
	public BitIndexEnumerator SetBits() => new(_bits);

	/// <summary>
	/// Set bits that have a definition.
	/// </summary>
	public BitIndexEnumerator DefinedSetBits() => new(_bits & _definedMask);

	public bool TryGetEntry(int bit, out BitmaskEntry entry)
	{
		if (_entries is not null && (uint)bit < 64)
		{
			ulong b = 1UL << bit;
			if ((_definedMask & b) != 0)
			{
				entry = _entries[BitHelper.PopCount(_definedMask & (b - 1))];
				return true;
			}
		}
		entry = default;
		return false;
	}

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? provider)
	{
		Span<char> stack = stackalloc char[256];
		if (TryFormat(stack, out int written, format.AsSpan(), provider))
		{
			return stack.Slice(0, written).ToString();
		}

		for (int size = 1024; ; size *= 2)
		{
			char[] rented = ArrayPool<char>.Shared.Rent(size);
			try
			{
				if (TryFormat(rented, out written, format.AsSpan(), provider))
				{
					return new string(rented, 0, written);
				}
			}
			finally
			{
				ArrayPool<char>.Shared.Return(rented);
			}
		}
	}

	public bool TryFormat(Span<char> destination, out int charsWritten,
		ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
	{
		char style = format.IsEmpty ? 'N' : format[0];
		switch (style)
		{
			case 'N': case 'n': return TryFormatNames(destination, out charsWritten, wire: false);
			case 'W': case 'w': return TryFormatNames(destination, out charsWritten, wire: true);
			case 'B': case 'b': return TryFormatBinary(destination, out charsWritten);
			default: throw new FormatException($"Unsupported bitmask format specifier '{style}'.");
		}
	}

	private bool TryFormatNames(Span<char> dest, out int charsWritten, bool wire)
	{
		charsWritten = 0;
		int pos = 0;

		if (_bits == 0)
		{
			if (!TryAppend(dest, ref pos, "(none)"))
			{
				return false;
			}
			charsWritten = pos;
			return true;
		}

		bool first = true;

		if (_entries is null)
		{
			if (!TryAppend(dest, ref pos, "["))
			{
				return false;
			}
			foreach (int bit in SetBits())
			{
				if (!first && !TryAppend(dest, ref pos, ", "))
				{
					return false;
				}
				if (!TryAppendUInt(dest, ref pos, (uint)bit))
				{
					return false;
				}
				first = false;
			}
			if (!TryAppend(dest, ref pos, "]"))
			{
				return false;
			}
			charsWritten = pos;
			return true;
		}

		foreach (int bit in DefinedSetBits())
		{
			if (!first && !TryAppend(dest, ref pos, ", "))
			{
				return false;
			}
			TryGetEntry(bit, out var entry);
			if (!TryAppend(dest, ref pos, wire ? entry.WireName : entry.Name))
			{
				return false;
			}
			first = false;
		}

		ulong unknown = UnknownBits;
		if (unknown != 0)
		{
			if (!first && !TryAppend(dest, ref pos, ", ")) return false;
			if (!TryAppend(dest, ref pos, "unknown:0x")) return false;
			if (!TryAppendHex(dest, ref pos, unknown)) return false;
		}

		charsWritten = pos;
		return true;
	}

	private bool TryFormatBinary(Span<char> dest, out int charsWritten)
	{
		int width = Width < 64 && (_bits >> Width) != 0 ? 64 : Width;
		charsWritten = 0;
		if (dest.Length < 2 + width)
		{
			return false;
		}
		dest[0] = '0';
		dest[1] = 'b';
		for (int i = 0; i < width; i++)
		{
			dest[2 + i] = ((_bits >> (width - 1 - i)) & 1) != 0 ? '1' : '0';
		}
		charsWritten = 2 + width;
		return true;
	}

	private static bool TryAppend(Span<char> dest, ref int pos, string s)
	{
		if (pos + s.Length > dest.Length)
		{
			return false;
		}
		s.AsSpan().CopyTo(dest.Slice(pos));
		pos += s.Length;
		return true;
	}

	private static bool TryAppendUInt(Span<char> dest, ref int pos, uint value)
	{
#if NETCOREAPP2_1_OR_GREATER
		if (!value.TryFormat(dest.Slice(pos), out int n, default, CultureInfo.InvariantCulture))
		{
			return false;
		}
		pos += n;
		return true;
#else
		Span<char> tmp = stackalloc char[10];
		int n = 0;
		do
		{
			tmp[n++] = (char)('0' + value % 10);
			value /= 10;
		} while (value != 0);

		if (pos + n > dest.Length)
		{
			return false;
		}
		for (int i = n - 1; i >= 0; i--)
		{
			dest[pos++] = tmp[i];
		}
		return true;
#endif
	}

	private static bool TryAppendHex(Span<char> dest, ref int pos, ulong value)
	{
#if NETCOREAPP2_1_OR_GREATER
		if (!value.TryFormat(dest.Slice(pos), out int n, "X", CultureInfo.InvariantCulture))
		{
			return false;
		}
		pos += n;
		return true;
#else
		Span<char> tmp = stackalloc char[16];
		int n = 0;
		do
		{
			int nibble = (int)(value & 0xF);
			tmp[n++] = (char)(nibble < 10 ? '0' + nibble : 'A' + nibble - 10);
			value >>= 4;
		} while (value != 0);

		if (pos + n > dest.Length)
		{
			return false;
		}
		for (int i = n - 1; i >= 0; i--)
		{
			dest[pos++] = tmp[i];
		}
		return true;
#endif
	}

	public bool Equals(BitmaskDisplay other) =>
		_bits == other._bits && _definedMask == other._definedMask &&
		Width == other.Width && ReferenceEquals(_entries, other._entries);
	public override bool Equals(object? obj) => obj is BitmaskDisplay other && Equals(other);
	public override int GetHashCode() => _bits.GetHashCode() ^ _definedMask.GetHashCode();
	public static bool operator ==(BitmaskDisplay l, BitmaskDisplay r) => l.Equals(r);
	public static bool operator !=(BitmaskDisplay l, BitmaskDisplay r) => !l.Equals(r);
}
