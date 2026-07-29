namespace Mavlink.Protocol.Generator;

public partial class MavlinkGenericEnumBitmaskGenerator
{
	internal static class Templates
	{
		internal const string BitmaskTemplate = @"
/// <summary>
/// Generic bitmask for the <see cref=""{{ namespace }}.{{ enum_name }}""/> enum.
/// </summary>
/// <remarks>
/// <see cref=""TUnderlying""/> can be any integer type (byte, sbyte, ushort, short, uint, int, ulong, long).
/// </remarks>
/// <example>
/// var bitmask = new {{ enum_name }}Bitmask&lt;ushort&gt;(0x0003);
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
    private readonly Lazy<System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>> _activeFlagsLazy;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<TUnderlying, System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>> _activeFlagsCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> struct with the specified bitmask value.
    /// Active flags are computed lazily on first access and cached for performance.
    /// </summary>
    /// <param name=""bitmask"">The bitmask value representing a combination of <see cref=""{{ namespace }}.{{ enum_name }}""/> flags.</param>
    /// <exception cref=""System.ArgumentOutOfRangeException"">Thrown when the bitmask contains bits outside the valid range of <see cref=""{{ namespace }}.{{ enum_name }}""/>.</exception>
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
        _activeFlagsLazy = new Lazy<System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>>(
            () => _activeFlagsCache.GetOrAdd(_bitmask, ComputeActiveFlags)
        );
    }

    /// <summary>
    /// Gets the underlying bitmask value representing the combination of enum flags.
    /// </summary>
    public TUnderlying Bitmask => _bitmask;

    /// <summary>
    /// Gets the <see cref=""{{ namespace }}.{{ enum_name }}""/> enum value interpreted from the bitmask.
    /// The bitmask is cast to the enum's base type ({{ enum_base_type }}) to ensure compatibility.
    /// </summary>
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
    private System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}> ComputeActiveFlags(TUnderlying bitmask)
    {
#if NET8_0_OR_GREATER
        if (bitmask == TUnderlying.Zero)
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        int count = bitmask switch
        {
            byte b => System.Numerics.BitOperations.PopCount(b),
            sbyte sb => System.Numerics.BitOperations.PopCount((byte)sb),
            ushort us => System.Numerics.BitOperations.PopCount(us),
            short s => System.Numerics.BitOperations.PopCount((ushort)s),
            uint u => System.Numerics.BitOperations.PopCount(u),
            int i => System.Numerics.BitOperations.PopCount((uint)i),
            ulong ul => System.Numerics.BitOperations.PopCount(ul),
            long l => System.Numerics.BitOperations.PopCount((ulong)l),
            _ => throw new System.InvalidOperationException(""Unsupported underlying type for PopCount."")
        };
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(count);
{{ for item in entries }}
        if ((bitmask & TUnderlying.CreateTruncating(({{ enum_base_type }}){{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }})) != TUnderlying.Zero)
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#elif NETCOREAPP3_0_OR_GREATER
        if (System.Collections.Generic.EqualityComparer<TUnderlying>.Default.Equals(bitmask, default))
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        int count = bitmask switch
        {
            byte b => System.Numerics.BitOperations.PopCount(b),
            sbyte sb => System.Numerics.BitOperations.PopCount((byte)sb),
            ushort us => System.Numerics.BitOperations.PopCount(us),
            short s => System.Numerics.BitOperations.PopCount((ushort)s),
            uint u => System.Numerics.BitOperations.PopCount(u),
            int i => System.Numerics.BitOperations.PopCount((uint)i),
            ulong ul => System.Numerics.BitOperations.PopCount(ul),
            long l => System.Numerics.BitOperations.PopCount((ulong)l),
            _ => System.Numerics.BitOperations.PopCount(Convert.ToUInt64(bitmask))
        };
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>(count);
{{ for item in entries }}
        if ((bitmask & (TUnderlying)({{ enum_base_type }}){{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }}) != default(TUnderlying))
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#else
        if (System.Collections.Generic.EqualityComparer<TUnderlying>.Default.Equals(bitmask, default))
        {
            return System.Collections.Immutable.ImmutableArray<{{ namespace }}.{{ enum_name }}>.Empty;
        }
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ namespace }}.{{ enum_name }}>({{ max_flags }});
{{ for item in entries }}
        if ((bitmask & (TUnderlying)({{ enum_base_type }}){{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }}) != default(TUnderlying))
        {
            builder.Add({{ namespace }}.{{ enum_name }}.{{ item.GeneratedName }});
        }
{{ end }}
        return builder.MoveToImmutable();
#endif
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance by comparing their bitmask values.
    /// </summary>
    /// <param name=""obj"">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is a <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> with the same bitmask; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is {{ enum_name }}Bitmask<TUnderlying> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> instance is equal to the current instance by comparing their bitmask values.
    /// </summary>
    /// <param name=""other"">The <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the bitmask values are equal; otherwise, <c>false</c>.</returns>
    public bool Equals({{ enum_name }}Bitmask<TUnderlying> other) => System.Collections.Generic.EqualityComparer<TUnderlying>.Default.Equals(_bitmask, other._bitmask);

    /// <summary>
    /// Returns the hash code for the bitmask value.
    /// </summary>
    /// <returns>A hash code based on the underlying bitmask value.</returns>
    public override int GetHashCode() => _bitmask.GetHashCode();

    /// <summary>
    /// Determines whether two <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> instances have the same bitmask value.
    /// </summary>
    /// <param name=""left"">The first instance to compare.</param>
    /// <param name=""right"">The second instance to compare.</param>
    /// <returns><c>true</c> if the bitmask values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==({{ enum_name }}Bitmask<TUnderlying> left, {{ enum_name }}Bitmask<TUnderlying> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref=""{{ enum_name }}Bitmask{TUnderlying}""/> instances have different bitmask values.
    /// </summary>
    /// <param name=""left"">The first instance to compare.</param>
    /// <param name=""right"">The second instance to compare.</param>
    /// <returns><c>true</c> if the bitmask values are different; otherwise, <c>false</c>.</returns>
    public static bool operator !=({{ enum_name }}Bitmask<TUnderlying> left, {{ enum_name }}Bitmask<TUnderlying> right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the bitmask and its active flags.
    /// </summary>
    /// <returns>A string in the format ""Bitmask: [value], Active flags: [flag1, flag2, ...]"".</returns>
    public override string ToString() => $""Bitmask: {_bitmask}, Active flags: [{string.Join("", "", ActiveFlags)}]"";
}";
	}
}
