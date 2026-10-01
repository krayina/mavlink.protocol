#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mavlink;

/// <summary>
/// Supplies <see cref="InvalidatableJsonConverter{T}"/> for MAVLink primitive type arguments
/// without reflection. For any other type argument, apply the closed converter to the member:
/// <c>[JsonConverter(typeof(InvalidatableJsonConverter&lt;MyEnum&gt;))]</c>.
/// </summary>
public sealed class InvalidatableJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Invalidatable<>);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type t = typeToConvert.GetGenericArguments()[0];

		// Every branch is a statically known instantiation, so NativeAOT compiles all of them.
		if (t == typeof(byte)) return new InvalidatableJsonConverter<byte>();
		if (t == typeof(sbyte)) return new InvalidatableJsonConverter<sbyte>();
		if (t == typeof(short)) return new InvalidatableJsonConverter<short>();
		if (t == typeof(ushort)) return new InvalidatableJsonConverter<ushort>();
		if (t == typeof(int)) return new InvalidatableJsonConverter<int>();
		if (t == typeof(uint)) return new InvalidatableJsonConverter<uint>();
		if (t == typeof(long)) return new InvalidatableJsonConverter<long>();
		if (t == typeof(ulong)) return new InvalidatableJsonConverter<ulong>();
		if (t == typeof(float)) return new InvalidatableJsonConverter<float>();
		if (t == typeof(double)) return new InvalidatableJsonConverter<double>();
		if (t == typeof(char)) return new InvalidatableJsonConverter<char>();

		throw new NotSupportedException(
			$"No reflection-free converter is available for {typeToConvert}. Annotate the member with " +
			$"[JsonConverter(typeof(InvalidatableJsonConverter<{t.Name}>))].");
	}
}
#endif
