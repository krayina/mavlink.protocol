using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IGeneratedStorage<T>
{
	/// <summary>
	/// Returns an immutable array of generated types that match the specified predicate.
	/// </summary>
	/// <param name="predicate">
	/// A function to test each generated type for a condition.
	/// If `null`, all generated types will be returned.
	/// </param>
	/// <returns>
	/// An immutable array of generated types that satisfy the condition specified by the predicate,
	/// or all types if the predicate is `null`.
	/// </returns>
	ImmutableArray<T> GetGeneratedTypes(Func<T, bool>? predicate);

	/// <summary>
	/// Returns an immutable array of all generated types.
	/// </summary>
	/// <returns>
	/// An immutable array of all generated types.
	/// </returns>
	ImmutableArray<T> GetGeneratedTypes();
}
