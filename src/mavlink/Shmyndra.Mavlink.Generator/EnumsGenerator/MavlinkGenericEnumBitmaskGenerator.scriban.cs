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
/// <see cref=""{{ underlying_type }}""/> must be an unsigned integer type (byte, ushort, uint, ulong).
/// </remarks>
/// <example>
/// var bitmask = new {{ enum_name }}Bitmask<ushort>(0x0003);
/// </example>
#if NET8_0_OR_GREATER
public readonly struct {{ enum_name }}Bitmask<{{ underlying_type }}> : IEnumBitmask<{{ enum_name }}, {{ underlying_type }}>
  where {{ underlying_type }} : struct, System.Numerics.IBinaryInteger<{{ underlying_type }}>
#else
public readonly struct {{ enum_name }}Bitmask<{{ underlying_type }}> : IEnumBitmask<{{ enum_name }}, {{ underlying_type }}>
  where {{ underlying_type }} : struct
#endif
{
    private readonly {{ underlying_type }} _bitmask;
    private readonly System.Collections.Immutable.ImmutableArray<{{ enum_name }}> _activeFlags;

    public {{ enum_name }}Bitmask({{ underlying_type }} bitmask)
    {
        if ((bitmask & ~{{ underlying_type }}.CreateTruncating({{ mask }})) != {{ underlying_type }}.Zero)
        {
            throw new System.ArgumentOutOfRangeException(nameof(bitmask), ""Bitmask contains bits outside the range of {{ enum_name }} ({{ enum_base_type }})."");
        }
        _bitmask = bitmask & {{ underlying_type }}.CreateTruncating({{ mask }});
        _activeFlags = default;
    }

    public {{ underlying_type }} Bitmask => _bitmask;

    public {{ enum_name }} Value
    {
        get
        {
#if NET8_0_OR_GREATER
            return ({{ enum_name }}){{ underlying_type }}.CreateTruncating(_bitmask);
#else
            return ({{ enum_name }})({{ enum_base_type }})_bitmask;
#endif
        }
    }

    public System.Collections.Immutable.ImmutableArray<{{ enum_name }}> ActiveFlags => _activeFlags.IsDefault ? ComputeActiveFlags() : _activeFlags;

    private System.Collections.Immutable.ImmutableArray<{{ enum_name }}> ComputeActiveFlags()
    {
#if NET8_0_OR_GREATER
        if (_bitmask == {{ underlying_type }}.Zero)
#else
        if (System.Collections.Generic.EqualityComparer<{{ underlying_type }}>.Default.Equals(_bitmask, default))
#endif
        {
            return System.Collections.Immutable.ImmutableArray<{{ enum_name }}>.Empty;
        }

#if NET9_0_OR_GREATER
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>(System.Numerics.BitOperations.PopCount(_bitmask));
        {{ underlying_type }} mask = _bitmask;
        while (mask != {{ underlying_type }}.Zero)
        {
            int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(mask);
            {{ underlying_type }} bitValue = {{ underlying_type }}.One << bitIndex;
            if (System.Enum.IsDefined(typeof({{ enum_name }}), ({{ enum_base_type }})bitValue))
            {
                builder.Add(({{ enum_name }})({{ enum_base_type }})bitValue);
            }
            mask &= ~bitValue;
        }
#else
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>({{ max_flags }});
{{ for item in entries }}
        if ((_bitmask & ({{ underlying_type }})({{ enum_base_type }}){{ enum_name }}.{{ item.GeneratedName }}) != 0)
        {
            builder.Add({{ enum_name }}.{{ item.GeneratedName }});
        }
{{ end }}
#endif
        return builder.MoveToImmutable();
    }

    public override bool Equals(object? obj) => obj is {{ enum_name }}Bitmask<{{ underlying_type }}> other && Equals(other);
    public bool Equals({{ enum_name }}Bitmask<{{ underlying_type }}> other) => System.Collections.Generic.EqualityComparer<{{ underlying_type }}>.Default.Equals(_bitmask, other._bitmask);
    public override int GetHashCode() => _bitmask.GetHashCode();
    public static bool operator ==({{ enum_name }}Bitmask<{{ underlying_type }}> left, {{ enum_name }}Bitmask<{{ underlying_type }}> right) => left.Equals(right);
    public static bool operator !=({{ enum_name }}Bitmask<{{ underlying_type }}> left, {{ enum_name }}Bitmask<{{ underlying_type }}> right) => !left.Equals(right);

    public override string ToString() => $""Bitmask: {_bitmask}, Active flags: [{string.Join("", "", ActiveFlags)}]"";
}";
	}
}
