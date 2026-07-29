#if NETCOREAPP3_1_OR_GREATER
namespace System.Text.Json.Serialization;

/// <summary>
/// A factory for creating <see cref="JsonConverter"/> instances for <see cref="Invalidatable{T}"/>.
/// </summary>
internal sealed class InvalidatableJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
		=> typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Invalidatable<>);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var valueType = typeToConvert.GetGenericArguments()[0];
		return (JsonConverter)Activator.CreateInstance(
			typeof(InvalidatableJsonConverter<>).MakeGenericType(valueType),
			[options])!;
	}
}
#endif
