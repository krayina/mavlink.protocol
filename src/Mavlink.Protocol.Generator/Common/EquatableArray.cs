using System.Collections;
using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator.Common;

/// <summary>
/// An immutable array compared by its elements, so pipeline models that hold collections
/// stay cacheable between generator runs.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

	// Null for default(EquatableArray<T>); treated as empty everywhere.
	private readonly T[]? _items;

	public EquatableArray(ImmutableArray<T> items)
	{
		_items = items.IsDefaultOrEmpty ? null : items.ToArray();
	}

	public EquatableArray(IEnumerable<T> items)
	{
		var array = items.ToArray();
		_items = array.Length == 0 ? null : array;
	}

	public int Count => _items?.Length ?? 0;

	public bool IsEmpty => Count == 0;

	public T this[int index] => (_items ?? throw new IndexOutOfRangeException())[index];

	public ImmutableArray<T> AsImmutableArray()
		=> _items is null ? ImmutableArray<T>.Empty : ImmutableArray.Create(_items);

	public bool Equals(EquatableArray<T> other)
	{
		var left = _items;
		var right = other._items;
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		if (left is null || right is null || left.Length != right.Length)
		{
			return false;
		}

		for (var i = 0; i < left.Length; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
			{
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		if (_items is null)
		{
			return 0;
		}

		unchecked
		{
			var hash = 17;
			foreach (var item in _items)
			{
				hash = (hash * 31) + (item is null ? 0 : EqualityComparer<T>.Default.GetHashCode(item));
			}

			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

	public static implicit operator EquatableArray<T>(ImmutableArray<T> items) => new(items);
}

internal static class EquatableArray
{
	public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
		where T : IEquatable<T>
		=> new(items);
}
