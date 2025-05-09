namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkGenericEnumBitmaskGenerator
{
	internal static class Templates
	{
		internal const string BitmaskTemplate = @"
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
            return _bitmask switch
            {
                byte b => ({{ enum_name }})({{ enum_base_type }})b,
                ushort us => ({{ enum_name }})({{ enum_base_type }})us,
                uint ui => ({{ enum_name }})({{ enum_base_type }})ui,
                ulong ul => ({{ enum_name }})({{ enum_base_type }})ul,
                _ => throw new InvalidOperationException(""Unsupported underlying type"")
            };
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

#if NET10_0_OR_GREATER
        BitField<{{ enum_name }}> bitField = new BitField<{{ enum_name }}>(_bitmask);
        return bitField.GetActiveFlags().ToImmutableArray();
#elif NET9_0_OR_GREATER
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>(System.Numerics.BitOperations.PopCount(_bitmask));
        {{ underlying_type }} mask = _bitmask;
        while (mask != {{ underlying_type }}.Zero)
        {
            int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(mask);
            {{ underlying_type }} bitValue = {{ underlying_type }}.One << bitIndex;
            if (System.Enum.IsDefined(typeof({{ enum_name }}), bitValue))
            {
                builder.Add(({{ enum_name }})bitValue);
            }
            mask &= ~bitValue;
        }
#elif NET8_0_OR_GREATER
        int count = int.CreateTruncating({{ underlying_type }}.PopCount(_bitmask));
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>(count);
{{ for item in entries }}
		if ((_bitmask & {{ underlying_type }}.CreateTruncating(({{ enum_base_type }}){{ enum_name }}.{{ item.GeneratedName }})) != {{ underlying_type }}.Zero)
		{
			builder.Add({{ enum_name }}.{{ item.GeneratedName }});
		}
{{ end }}
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
