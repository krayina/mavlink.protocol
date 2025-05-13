namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkGenericEnumBitmaskGenerator
{
	internal static class Templates
	{
		internal const string BitmaskTemplate = @"
/// <summary>
/// Generic bitmask for {{ enum_name }}.
/// </summary>
/// <remarks>
/// <see cref=""TUnderlying""/> must be an unsigned integer type (byte, ushort, uint, ulong).
/// </remarks>
/// <example>
/// var bitmask = new {{ enum_name }}Bitmask<ushort>(0x0003);
/// </example>
#if NET8_0_OR_GREATER
public readonly struct {{ enum_name }}Bitmask<TUnderlying> : EnumBitmask.IEnumBitmask<{{ namespace }}.{{ enum_name }}, TUnderlying>
    where TUnderlying : struct, System.Numerics.IBinaryInteger<TUnderlying>
#else
public readonly struct {{ enum_name }}Bitmask<TUnderlying> : EnumBitmask.IEnumBitmask<{{ namespace }}.{{ enum_name }}, TUnderlying>
    where TUnderlying : struct
#endif
{
    private readonly TUnderlying _bitmask;
    private readonly System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> _activeFlags;

    public {{ enum_name }}Bitmask(TUnderlying bitmask)
    {
#if NET8_0_OR_GREATER
        if ((bitmask & ~TUnderlying.CreateTruncating({{ mask }})) != TUnderlying.Zero)
#else
        if ((bitmask & ~(TUnderlying)({{ enum_base_type }})({{ mask }})) != default(TUnderlying))
#endif
        {
            throw new System.ArgumentOutOfRangeException(nameof(bitmask), ""Bitmask contains bits outside the range of {{ namespace }}.{{ enum_name }} ({{ enum_base_type }})."");
        }
#if NET8_0_OR_GREATER
        _bitmask = bitmask & TUnderlying.CreateTruncating({{ mask }});
#else
        _bitmask = bitmask & (TUnderlying)({{ enum_base_type }})({{ mask }});
#endif
        _activeFlags = default;
    }

    public TUnderlying Bitmask => _bitmask;

    public {{ namespace }}.{{ enum_name }} Value
    {
        get
        {
#if NET8_0_OR_GREATER
            return ({{ namespace }}.{{ enum_name }})TUnderlying.CreateTruncating(_bitmask);
#else
            return ({{ namespace }}.{{ enum_name }})({{ enum_base_type }})_bitmask;
#endif
        }
    }

    public System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> ActiveFlags => _activeFlags.IsDefault ? ComputeActiveFlags() : _activeFlags;

    private System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> ComputeActiveFlags()
    {
#if NET8_0_OR_GREATER
        if (_bitmask == TUnderlying.Zero)
#else
        if (System.Collections.Generic.EqualityComparer<TUnderlying>.Default.Equals(_bitmask, default))
#endif
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }

#if NET9_0_OR_GREATER
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(System.Numerics.BitOperations.PopCount(_bitmask));
        TUnderlying mask = _bitmask;
        while (mask != TUnderlying.Zero)
        {
            int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(mask);
            TUnderlying bitValue = TUnderlying.One << bitIndex;
            if (System.Enum.IsDefined(typeof({{ namespace }}.{{ enum_name }}), ({{ enum_base_type }})bitValue))
            {
                builder.Add(({{ namespace }}.{{ enum_name }})({{ enum_base_type }})bitValue);
            }
            mask &= ~bitValue;
        }
#else
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>({{ max_flags }});
{{ for item in entries }}
        if ((_bitmask & (TUnderlying)({{ enum_base_type }}){{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }}) != 0)
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }});
        }
{{ end }}
#endif
        return builder.MoveToImmutable();
    }

    public override bool Equals(object? obj) => obj is {{ enum_name }}Bitmask<TUnderlying> other && Equals(other);
    public bool Equals({{ enum_name }}Bitmask<TUnderlying> other) => System.Collections.Generic.EqualityComparer<TUnderlying>.Default.Equals(_bitmask, other._bitmask);
    public override int GetHashCode() => _bitmask.GetHashCode();
    public static bool operator ==({{ enum_name }}Bitmask<TUnderlying> left, {{ enum_name }}Bitmask<TUnderlying> right) => left.Equals(right);
    public static bool operator !=({{ enum_name }}Bitmask<TUnderlying> left, {{ enum_name }}Bitmask<TUnderlying> right) => !left.Equals(right);

    public override string ToString() => $""Bitmask: {_bitmask}, Active flags: [{string.Join("", "", ActiveFlags)}]"";
}";
	}
}
