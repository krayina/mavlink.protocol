#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Mavlink;

/// <summary>
/// Converts <see cref="Invalidatable{T}"/> to and from JSON: a valid value is written as the inner
/// value, an invalid one as <c>null</c>. Parameterless and reflection-free, so it can be applied via
/// <see cref="JsonConverterAttribute"/> with a closed type argument and used under NativeAOT.
/// </summary>
/// <remarks>
/// MAVLink primitive types use built-in converters directly. Any other <typeparamref name="T"/>
/// is resolved through the options' type info resolver; with a source-generated context,
/// <typeparamref name="T"/> must be registered there (for example <c>[JsonSerializable(typeof(MyEnum))]</c>).
/// </remarks>
public sealed class InvalidatableJsonConverter<T> : JsonConverter<Invalidatable<T>>
{
	// Null for non-primitive T. Computed once per closed instantiation.
	private static readonly JsonConverter<T>? _primitive = GetPrimitiveConverter();

	public override Invalidatable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return Invalidatable<T>.Invalid;
		}

		T value = _primitive is { } converter
			? converter.Read(ref reader, typeof(T), options)!
			: JsonSerializer.Deserialize(ref reader, GetTypeInfo(options))!;

		return Invalidatable<T>.From(value);
	}

	public override void Write(Utf8JsonWriter writer, Invalidatable<T> value, JsonSerializerOptions options)
	{
		if (!value.TryGetValue(out var inner))
		{
			writer.WriteNullValue();
			return;
		}

		if (_primitive is { } converter)
		{
			converter.Write(writer, inner, options);
		}
		else
		{
			JsonSerializer.Serialize(writer, inner, GetTypeInfo(options));
		}
	}

	private static JsonTypeInfo<T> GetTypeInfo(JsonSerializerOptions options) =>
		(JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));

	private static JsonConverter<T>? GetPrimitiveConverter()
	{
		if (typeof(T) == typeof(byte)) return (JsonConverter<T>)(object)JsonMetadataServices.ByteConverter;
		if (typeof(T) == typeof(sbyte)) return (JsonConverter<T>)(object)JsonMetadataServices.SByteConverter;
		if (typeof(T) == typeof(short)) return (JsonConverter<T>)(object)JsonMetadataServices.Int16Converter;
		if (typeof(T) == typeof(ushort)) return (JsonConverter<T>)(object)JsonMetadataServices.UInt16Converter;
		if (typeof(T) == typeof(int)) return (JsonConverter<T>)(object)JsonMetadataServices.Int32Converter;
		if (typeof(T) == typeof(uint)) return (JsonConverter<T>)(object)JsonMetadataServices.UInt32Converter;
		if (typeof(T) == typeof(long)) return (JsonConverter<T>)(object)JsonMetadataServices.Int64Converter;
		if (typeof(T) == typeof(ulong)) return (JsonConverter<T>)(object)JsonMetadataServices.UInt64Converter;
		if (typeof(T) == typeof(float)) return (JsonConverter<T>)(object)JsonMetadataServices.SingleConverter;
		if (typeof(T) == typeof(double)) return (JsonConverter<T>)(object)JsonMetadataServices.DoubleConverter;
		if (typeof(T) == typeof(char)) return (JsonConverter<T>)(object)JsonMetadataServices.CharConverter;
		return null;
	}
}
#endif
