using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public interface IGeneratedStorage<T>
{
	/// <summary>
	/// Returns an immutable array of generated types that match the specified predicate.
	/// If the predicate is not provided, the method will return all generated types.
	/// </summary>
	/// <param name="predicate">
	/// A predicate function to filter the generated types by their keys (Namespace, Name).
	/// If null, all generated types will be returned.
	/// </param>
	/// <returns>
	/// An immutable array of generated types that match the predicate, or all types if the predicate is null.
	/// </returns>
	ImmutableArray<T> GetGeneratedTypes(Func<(string Namespace, string Name), bool> predicate);

	/// <summary>
	/// Returns an immutable array of all generated types.
	/// </summary>
	/// <returns>
	/// An immutable array of all generated types.
	/// </returns>
	ImmutableArray<T> GetGeneratedTypes();
}
