namespace Mavlink.Protocol.Generator;

public partial class MavlinkSpecificEnumBitmaskGenerator
{
	internal static class Templates
	{
		internal const string SpecificBitmaskTemplate = @"
/// <summary>
/// A readonly struct that encapsulates a bitmask for the <see cref=""{{ namespace }}.{{ enum_name }}""/> enum,
/// allowing manipulation and inspection of enum flags using an underlying <see cref=""{{ underlying_type }}""/> type.
/// Implements <see cref=""EnumBitmask.IEnumBitmask{TEnum, TUnderlying}""/> for type-safe bitmask operations.
/// </summary>
public readonly struct {{ struct_name }} : EnumBitmask.IEnumBitmask<{{ namespace }}.{{ enum_name }}, {{ underlying_type }}>
{
    private readonly {{ underlying_type }} _bitmask;
    private readonly Lazy<System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>> _activeFlagsLazy;

    private const {{ underlying_type }} ValidMask = {{ valid_mask }};
    private const int EnumValuesCount = {{ enum_values_count }};

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<{{ underlying_type }}, System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>> _activeFlagsCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref=""{{ struct_name }}""/> struct with the specified bitmask value.
    /// Active flags are computed lazily on first access and cached for performance.
    /// </summary>
    /// <param name=""bitmask"">The bitmask value representing a combination of <see cref=""{{ namespace }}.{{ enum_name }}""/> flags.</param>
    /// <exception cref=""System.ArgumentOutOfRangeException"">Thrown when the bitmask contains bits outside the valid range of <see cref=""{{ namespace }}.{{ enum_name }}""/>.</exception>
    public {{ struct_name }}({{ underlying_type }} bitmask)
    {
        if ((bitmask & ~ValidMask) != 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(bitmask), ""Bitmask contains bits outside the range of {{ namespace }}.{{ enum_name }}."");
        }
        _bitmask = bitmask;
        _activeFlagsLazy = new Lazy<System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>>(
            () => _activeFlagsCache.GetOrAdd(_bitmask, ComputeActiveFlags)
        );
    }

    /// <summary>
    /// Gets the underlying bitmask value representing the combination of enum flags.
    /// </summary>
    public {{ underlying_type }} Bitmask => _bitmask;

    /// <summary>
    /// Gets the <see cref=""{{ namespace }}.{{ enum_name }}""/> enum value interpreted from the bitmask.
    /// This is a direct cast of the bitmask to the enum's base type.
    /// </summary>
    public {{ namespace }}.{{ enum_name }} Value => ({{ namespace }}.{{ enum_name }})({{ enum_base_type }})_bitmask;

    /// <summary>
    /// Gets an immutable array of active <see cref=""{{ namespace }}.{{ enum_name }}""/> flags set in the bitmask.
    /// The flags are computed on first access and cached locally for subsequent accesses to optimize performance.
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> ActiveFlags => _activeFlagsLazy.Value;

    /// <summary>
    /// Computes the active flags from the specified bitmask value.
    /// </summary>
    /// <param name=""bitmask"">The bitmask value to analyze.</param>
    /// <returns>An immutable array containing the <see cref=""{{ namespace }}.{{ enum_name }}""/> flags that are set in the bitmask.</returns>
    private static System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> ComputeActiveFlags({{ underlying_type }} bitmask)
    {
#if NET8_0_OR_GREATER
        if (bitmask == 0)
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        int count = {{ if underlying_type == 'sbyte' }}System.Numerics.BitOperations.PopCount((byte)(sbyte)bitmask)
                    {{ elif underlying_type == 'short' }}System.Numerics.BitOperations.PopCount((ushort)(short)bitmask)
                    {{ elif underlying_type == 'int' }}System.Numerics.BitOperations.PopCount((uint)(int)bitmask)
                    {{ elif underlying_type == 'long' }}System.Numerics.BitOperations.PopCount((ulong)(long)bitmask)
                    {{ else }}{{ underlying_type }}.PopCount(bitmask){{ end }};
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(count);
{{ for entry in entries }}
        if ((bitmask & ({{ underlying_type }}){{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }}) != 0)
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#elif NETCOREAPP3_0_OR_GREATER
        if (bitmask == 0)
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        int count = {{ if underlying_type == 'sbyte' }}System.Numerics.BitOperations.PopCount((byte)(sbyte)bitmask)
                    {{ elif underlying_type == 'short' }}System.Numerics.BitOperations.PopCount((ushort)(short)bitmask)
                    {{ elif underlying_type == 'int' }}System.Numerics.BitOperations.PopCount((uint)(int)bitmask)
                    {{ elif underlying_type == 'long' }}System.Numerics.BitOperations.PopCount((ulong)(long)bitmask)
                    {{ else }}System.Numerics.BitOperations.PopCount((ulong)bitmask){{ end }};
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(count);
{{ for entry in entries }}
        if ((bitmask & ({{ underlying_type }}){{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }}) != 0)
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#else
        if (bitmask == 0)
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(EnumValuesCount);
{{ for entry in entries }}
        if ((bitmask & ({{ underlying_type }}){{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }}) != 0)
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ entry.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#endif
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance by comparing their bitmask values.
    /// </summary>
    /// <param name=""obj"">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is a <see cref=""{{ struct_name }}""/> with the same bitmask; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is {{ struct_name }} other && _bitmask == other._bitmask;

    /// <summary>
    /// Determines whether the specified <see cref=""{{ struct_name }}""/> instance is equal to the current instance by comparing their bitmask values.
    /// </summary>
    /// <param name=""other"">The <see cref=""{{ struct_name }}""/> instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the bitmask values are equal; otherwise, <c>false</c>.</returns>
    public bool Equals({{ struct_name }} other) => _bitmask == other._bitmask;

    /// <summary>
    /// Returns the hash code for the bitmask value.
    /// </summary>
    /// <returns>A hash code based on the underlying bitmask value.</returns>
    public override int GetHashCode() => _bitmask.GetHashCode();

    /// <summary>
    /// Determines whether two <see cref=""{{ struct_name }}""/> instances have the same bitmask value.
    /// </summary>
    /// <param name=""left"">The first instance to compare.</param>
    /// <param name=""right"">The second instance to compare.</param>
    /// <returns><c>true</c> if the bitmask values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==({{ struct_name }} left, {{ struct_name }} right) => left._bitmask == right._bitmask;

    /// <summary>
    /// Determines whether two <see cref=""{{ struct_name }}""/> instances have different bitmask values.
    /// </summary>
    /// <param name=""left"">The first instance to compare.</param>
    /// <param name=""right"">The second instance to compare.</param>
    /// <returns><c>true</c> if the bitmask values are different; otherwise, <c>false</c>.</returns>
    public static bool operator !=({{ struct_name }} left, {{ struct_name }} right) => left._bitmask != right._bitmask;

    /// <summary>
    /// Returns a string representation of the bitmask and its active flags.
    /// </summary>
    /// <returns>A string in the format ""Bitmask: [value], Active flags: [flag1, flag2, ...]"".</returns>
    public override string ToString() => $""Bitmask: {_bitmask}, Active flags: [{string.Join("", "", ActiveFlags)}]"";
}";
	}
}
