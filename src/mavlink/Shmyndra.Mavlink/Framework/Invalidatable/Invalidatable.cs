#if NET8_0_OR_GREATER
using System.Numerics;
#endif
#if NETCOREAPP3_1_OR_GREATER
using System.Text.Json.Serialization;
#endif
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System;

/// <summary>
/// Represents a value that can be in an invalid state.
/// This struct is used to distinguish an invalid state from a 'null' value,
/// which might signify an absent optional field (e.g., in MAVLink extensions).
/// </summary>
/// <remarks>
/// This is a readonly struct to prevent heap allocations. A default instance is considered invalid.
/// For XML serialization, it is recommended to use a surrogate property pattern on the containing class
/// instead of relying on the default IXmlSerializable implementation, which has limitations with immutable structs.
/// </remarks>
/// <typeparam name="T">The type of the value to wrap.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
#if NETCOREAPP3_1_OR_GREATER
[JsonConverter(typeof(InvalidatableJsonConverterFactory))]
#endif
public readonly struct Invalidatable<T> : IEquatable<Invalidatable<T>>, IXmlSerializable
#if NET7_0_OR_GREATER
	, IEqualityOperators<Invalidatable<T>, Invalidatable<T>, bool>
#endif
{
	private readonly T _value;
	private readonly bool _isValid;

	/// <summary>
	/// Gets the value if it is valid.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the instance is in an invalid state.</exception>
	public T Value => _isValid ? _value : throw new InvalidOperationException("Cannot access the value of an invalid instance.");

	/// <summary>
	/// Gets a value indicating whether the current instance holds a valid value.
	/// </summary>
	public bool IsValid => _isValid;

	/// <summary>
	/// Represents an invalid instance of <see cref="Invalidatable{T}"/>.
	/// </summary>
	public static Invalidatable<T> Invalid => default;

	private Invalidatable(T value)
	{
		_value = value;
		_isValid = true;
	}

	/// <summary>
	/// Creates a new valid instance of <see cref="Invalidatable{T}"/>.
	/// </summary>
	/// <param name="value">The value to wrap. A null value for a reference type is considered a valid state.</param>
	public static Invalidatable<T> From(T value) => new Invalidatable<T>(value);

	/// <summary>
	/// Implicitly converts a value to a valid <see cref="Invalidatable{T}"/>.
	/// </summary>
	public static implicit operator Invalidatable<T>(T value) => new Invalidatable<T>(value);

	/// <summary>
	/// Safely gets the value without throwing an exception.
	/// </summary>
	/// <param name="value">The wrapped value if the instance is valid; otherwise, the default value for <typeparamref name="T"/>.</param>
	/// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
	public bool TryGetValue([Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T value)
#else
	public bool TryGetValue(out T value)
#endif
	{
		value = _value;
		return _isValid;
	}

	/// <summary>
	/// Gets the value if valid, or a default value otherwise.
	/// </summary>
	/// <param name="defaultValue">The value to return if the instance is invalid.</param>
	/// <returns>The wrapped value or the provided default.</returns>
	public T GetValueOrDefault(T defaultValue) => _isValid ? _value : defaultValue;

	public override bool Equals(object? obj) => obj is Invalidatable<T> other && Equals(other);

	public bool Equals(Invalidatable<T> other)
	{
		if (!_isValid && !other._isValid)
		{
			return true;
		}
		if (_isValid != other._isValid)
		{
			return false;
		}
		return EqualityComparer<T>.Default.Equals(_value, other._value);
	}

	public override int GetHashCode()
	{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return _isValid ? HashCode.Combine(_value, _isValid) : 0;
#else
		if (!_isValid)
		{
			return 0;
		}

		unchecked
		{
			int hashCode = 17;
			hashCode = hashCode * 23 + (_value == null ? 0 : _value.GetHashCode());
			hashCode = hashCode * 23 + _isValid.GetHashCode();
			return hashCode;
		}
#endif
	}

	public override string ToString() => _isValid ? _value?.ToString() ?? "null" : "[Invalid]";
	public static bool operator ==(Invalidatable<T> left, Invalidatable<T> right) => left.Equals(right);
	public static bool operator !=(Invalidatable<T> left, Invalidatable<T> right) => !left.Equals(right);

	#region IXmlSerializable Implementation
	XmlSchema? IXmlSerializable.GetSchema() => null;
	void IXmlSerializable.ReadXml(XmlReader reader) => throw new NotSupportedException("Deserialization via IXmlSerializable is not supported for this immutable struct. Use a surrogate property on the containing class.");
	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (IsValid)
		{
			new XmlSerializer(typeof(T)).Serialize(writer, Value);
		}
	}
	#endregion
}
