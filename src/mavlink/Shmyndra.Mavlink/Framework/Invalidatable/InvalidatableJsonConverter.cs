#if NETCOREAPP3_1_OR_GREATER
namespace System.Text.Json.Serialization;

/// <summary>
/// Converts an <see cref="Invalidatable{T}"/> to and from JSON, representing it as the inner value or null.
/// </summary>
internal sealed class InvalidatableJsonConverter<T> : JsonConverter<Invalidatable<T>>
{
	private readonly JsonConverter<T> _valueConverter;
	public InvalidatableJsonConverter(JsonSerializerOptions options)
	{
		_valueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
	}

	public override Invalidatable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return Invalidatable<T>.Invalid;
		}
		var value = _valueConverter.Read(ref reader, typeof(T), options);
		return Invalidatable<T>.From(value!);
	}

	public override void Write(Utf8JsonWriter writer, Invalidatable<T> value, JsonSerializerOptions options)
	{
		if (value.TryGetValue(out var innerValue))
		{
			_valueConverter.Write(writer, innerValue, options);
		}
		else
		{
			writer.WriteNullValue();
		}
	}
}
#endif
