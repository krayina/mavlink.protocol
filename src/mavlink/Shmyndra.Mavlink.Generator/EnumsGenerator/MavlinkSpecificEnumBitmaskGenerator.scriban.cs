namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkSpecificEnumBitmaskGenerator
{
	internal static class Templates
	{
		internal const string SpecificBitmaskTemplate = @"
public readonly struct {{ struct_name }} : IEnumBitmask<{{ enum_name }}, {{ underlying_type }}>
{
    private readonly {{ underlying_type }} _bitmask;
    private readonly System.Collections.Immutable.ImmutableArray<{{ enum_name }}> _activeFlags;

    public {{ struct_name }}({{ underlying_type }} bitmask)
    {
        if ((bitmask & {{ underlying_type }}.CreateTruncating({{ mask }})) != {{ underlying_type }}.Zero)
        {
            throw new System.ArgumentOutOfRangeException(nameof(bitmask), ""Bitmask contains bits outside the range of {{ enum_name }} ({{ enum_base_type }})."");
        }
        _bitmask = bitmask;
        _activeFlags = default;
    }

    public {{ underlying_type }} Bitmask => _bitmask;

    public {{ enum_name }} Value => ({{ enum_name }})({{ enum_base_type }})_bitmask;

    public System.Collections.Immutable.ImmutableArray<{{ enum_name }}> ActiveFlags => _activeFlags.IsDefault ? ComputeActiveFlags() : _activeFlags;

    private System.Collections.Immutable.ImmutableArray<{{ enum_name }}> ComputeActiveFlags()
    {
#if NET9_0_OR_GREATER
        if (_bitmask == {{ underlying_type }}.Zero)
        {
            return System.Collections.Immutable.ImmutableArray<{{ enum_name }}>.Empty;
        }
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>({{ underlying_type }}.PopCount(_bitmask));
        {{ underlying_type }} mask = _bitmask;
        while (mask != {{ underlying_type }}.Zero)
        {
            int bitIndex = {{ underlying_type }}.TrailingZeroCount(mask);
            {{ underlying_type }} bitValue = ({{ underlying_type }})(1 << bitIndex);
            if (System.Enum.IsDefined(typeof({{ enum_name }}), ({{ enum_base_type }})bitValue))
            {
                builder.Add(({{ enum_name }})({{ enum_base_type }})bitValue);
            }
            mask &= ({{ underlying_type }})~bitValue;
        }
        return builder.MoveToImmutable();
#elif NET8_0_OR_GREATER
        if (_bitmask == {{ underlying_type }}.Zero)
        {
            return System.Collections.Immutable.ImmutableArray<{{ enum_name }}>.Empty;
        }
        int count = System.Numerics.BitOperations.PopCount(_bitmask);
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>(count);
{{ for entry in entries }}
        if ((_bitmask & {{ underlying_type }}.CreateTruncating(({{ enum_base_type }}){{ enum_name }}.{{ entry.GeneratedName }})) != {{ underlying_type }}.Zero)
        {
            builder.Add({{ enum_name }}.{{ entry.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#else
        if (System.Collections.Generic.EqualityComparer<{{ underlying_type }}>.Default.Equals(_bitmask, default))
        {
            return System.Collections.Immutable.ImmutableArray<{{ enum_name }}>.Empty;
        }
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ enum_name }}>({{ max_flags }});
{{ for entry in entries }}
        if ((_bitmask & ({{ underlying_type }})({{ enum_base_type }}){{ enum_name }}.{{ entry.GeneratedName }}) != 0)
        {
            builder.Add({{ enum_name }}.{{ entry.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#endif
    }

    public override bool Equals(object? obj) => obj is {{ struct_name }} other && Equals(other);
    public bool Equals({{ struct_name }} other) => System.Collections.Generic.EqualityComparer<{{ underlying_type }}>.Default.Equals(_bitmask, other._bitmask);
    public override int GetHashCode() => _bitmask.GetHashCode();
    public static bool operator ==({{ struct_name }} left, {{ struct_name }} right) => left.Equals(right);
    public static bool operator !=({{ struct_name }} left, {{ struct_name }} right) => !left.Equals(right);

    public override string ToString() => $""Bitmask: {_bitmask}, Active flags: [{string.Join("", "", ActiveFlags)}]"";
}";
	}
}
