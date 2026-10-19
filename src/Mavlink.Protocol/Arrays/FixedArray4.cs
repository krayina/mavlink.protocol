using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Mavlink;

/// <summary>
/// Fixed-length array of 4 elements stored inline: the C# shape of a MAVLink <c>T[4]</c> field.
/// </summary>
/// <remarks>
/// <para>
/// No heap allocation, value semantics (element-wise equality, so DTO record equality
/// works), and <c>default</c> is a valid all-zero array — exactly what a zero-filled
/// payload decodes to. The length is part of the type, so a wrongly sized array cannot
/// be assigned to a message field at all; the only runtime length check is the span
/// constructor.
/// </para>
/// <para>
/// One implementation for every TFM: explicit fields rather than <c>[InlineArray]</c>,
/// which keeps the public shape identical everywhere and works on netstandard2.0.
/// The span view (<see cref="AsSpan"/>) needs a fast span and is absent on netstandard2.0;
/// indexer, <see cref="CopyTo"/> and enumeration work everywhere.
/// </para>
/// <para>
/// Built with a collection expression: <c>Temperature = [2500, 2510, Invalidatable&lt;short&gt;.Invalid, 2490]</c>.
/// </para>
/// </remarks>
[CollectionBuilder(typeof(FixedArray4), nameof(FixedArray4.Create))]
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct FixedArray4<T> : IEquatable<FixedArray4<T>>, IReadOnlyList<T>
	where T : unmanaged
{
	private const int N = 4;

	// Identical unmanaged fields under sequential layout are laid out contiguously with
	// stride sizeof(T) (no GC references, so the runtime cannot reorder them): the
	// Unsafe.Add-based element access below relies on that.
	private readonly T _e0, _e1, _e2, _e3;

	public FixedArray4(T e0, T e1, T e2, T e3)
	{
		_e0 = e0;
		_e1 = e1;
		_e2 = e2;
		_e3 = e3;
	}

	/// <summary>
	/// Copies exactly 4 elements from <paramref name="values"/>.
	/// </summary>
	/// <exception cref="ArgumentException"><paramref name="values"/> is not 4 elements long.</exception>
	public FixedArray4(ReadOnlySpan<T> values)
	{
		if (values.Length != N)
		{
			throw CodecThrowHelper.LengthMismatch(N, values.Length);
		}

		_e0 = values[0];
		_e1 = values[1];
		_e2 = values[2];
		_e3 = values[3];
	}

	public int Length => N;

	int IReadOnlyCollection<T>.Count => N;

	public T this[int index]
	{
		get
		{
			if ((uint)index >= N)
			{
				throw CodecThrowHelper.IndexOutOfRange(index, N);
			}
			return Unsafe.Add(ref Unsafe.AsRef(in _e0), index);
		}
	}

	/// <summary>
	/// Returns a copy with the element at <paramref name="index"/> replaced.
	/// </summary>
	public FixedArray4<T> With(int index, T value)
	{
		if ((uint)index >= N)
		{
			throw CodecThrowHelper.IndexOutOfRange(index, N);
		}

		FixedArray4<T> copy = this;
		Unsafe.Add(ref Unsafe.AsRef(in copy._e0), index) = value;
		return copy;
	}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
	/// <summary>
	/// The elements as a span pointing into this value; the compiler prevents it from outliving it.
	/// </summary>
	[UnscopedRef]
	public ReadOnlySpan<T> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _e0), N);
#endif

	public void CopyTo(Span<T> destination)
	{
		if (destination.Length < N)
		{
			throw CodecThrowHelper.DestinationTooShort(N, destination.Length);
		}

		destination[0] = _e0;
		destination[1] = _e1;
		destination[2] = _e2;
		destination[3] = _e3;
	}

	public T[] ToArray() => new[] { _e0, _e1, _e2, _e3 };

	public Enumerator GetEnumerator() => new(this);

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// Element-wise on purpose: a bytewise compare would read padding inside T
	// (e.g. Invalidatable<short>) and would treat NaN payloads as different values.
	public bool Equals(FixedArray4<T> other)
	{
		var comparer = EqualityComparer<T>.Default;
		return comparer.Equals(_e0, other._e0)
			&& comparer.Equals(_e1, other._e1)
			&& comparer.Equals(_e2, other._e2)
			&& comparer.Equals(_e3, other._e3);
	}

	public override bool Equals(object? obj) => obj is FixedArray4<T> other && Equals(other);

	public override int GetHashCode()
	{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		return HashCode.Combine(_e0, _e1, _e2, _e3);
#else
		var comparer = EqualityComparer<T>.Default;
		unchecked
		{
			int hash = 17;
			hash = (hash * 31) + comparer.GetHashCode(_e0);
			hash = (hash * 31) + comparer.GetHashCode(_e1);
			hash = (hash * 31) + comparer.GetHashCode(_e2);
			hash = (hash * 31) + comparer.GetHashCode(_e3);
			return hash;
		}
#endif
	}

	public static bool operator ==(FixedArray4<T> left, FixedArray4<T> right) => left.Equals(right);

	public static bool operator !=(FixedArray4<T> left, FixedArray4<T> right) => !left.Equals(right);

	public override string ToString()
		=> "[" + _e0.ToString() + ", " + _e1.ToString() + ", " + _e2.ToString() + ", " + _e3.ToString() + "]";

	/// <summary>
	/// Allocation-free enumerator over a copy of the array.
	/// </summary>
	[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
		Justification = "Mutable iteration cursor; comparing enumerators is meaningless.")]
	public struct Enumerator : IEnumerator<T>
	{
		private readonly FixedArray4<T> _array;
		private int _index;

		internal Enumerator(FixedArray4<T> array)
		{
			_array = array;
			_index = -1;
		}

		public readonly T Current => _array[_index];

		readonly object IEnumerator.Current => Current;

		public bool MoveNext() => ++_index < N;

		public void Reset() => _index = -1;

		public readonly void Dispose()
		{
		}
	}
}

/// <summary>
/// Factory for <see cref="FixedArray4{T}"/>; also the collection-expression builder.
/// </summary>
public static class FixedArray4
{
	/// <inheritdoc cref="FixedArray4{T}(ReadOnlySpan{T})"/>
	public static FixedArray4<T> Create<T>(ReadOnlySpan<T> values) where T : unmanaged => new(values);
}
