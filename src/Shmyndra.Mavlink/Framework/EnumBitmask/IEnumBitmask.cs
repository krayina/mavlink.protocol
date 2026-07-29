using System.Collections.Immutable;

namespace EnumBitmask;

/// <summary>
/// Represents a bitmask based on an enumeration with a specified underlying type.
/// </summary>
/// <typeparam name="T">The enumeration type that defines the bitmask flags.</typeparam>
/// <typeparam name="TUnderlying">The underlying numeric type of the enumeration (e.g., byte, ushort, uint, ulong).</typeparam>
public interface IEnumBitmask<T, TUnderlying>
	where T : struct, Enum
	where TUnderlying : struct
{
	/// <summary>
	/// Gets the raw numeric value of the bitmask as its underlying type.
	/// </summary>
	TUnderlying Bitmask { get; }

	/// <summary>
	/// Gets the bitmask value interpreted as the enumeration type.
	/// </summary>
	T Value { get; }

	/// <summary>
	/// Gets an immutable array of active flags in the bitmask.
	/// This collection is lazily initialized on first access.
	/// </summary>
	ImmutableArray<T> ActiveFlags { get; }
}
