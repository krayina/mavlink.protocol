using System.Collections.Concurrent;

namespace Shmyndra.Mavlink.Generator;

public static class InvalidFieldHandlerFactory
{
	private static readonly ConcurrentDictionary<string, IInvalidFieldHandler> _cache =
		new ConcurrentDictionary<string, IInvalidFieldHandler>(StringComparer.OrdinalIgnoreCase);

	public static IInvalidFieldHandler? Create(GeneratedMavlinkMessageField field)
	{
		if (string.IsNullOrWhiteSpace(field.Original.Invalid))
		{
			return null;
		}

		string invalid = field.Original.Invalid!;

		return field.GeneratedType switch
		{
			GeneratedMavlinkMessageFieldArrayType arrayType when invalid.StartsWith("[") =>
				_cache.GetOrAdd(invalid, _ => new ArrayInvalidFieldHandler(invalid, arrayType.ArrayLength)),
			GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when invalid.StartsWith("[") =>
				_cache.GetOrAdd(invalid, _ => new ArrayInvalidFieldHandler(invalid, arrayEnumType.ArrayLength)),
			GeneratedMavlinkMessageFieldEnumType enumType => CreateForEnumType(enumType, invalid),
			GeneratedMavlinkMessageFieldPrimitiveType simple => CreateForSimpleType(simple, invalid),
			_ => throw new NotSupportedException($"Invalid condition '{invalid}' is not supported for type {field.GeneratedType}")
		};
	}

	private static IInvalidFieldHandler CreateForSimpleType(GeneratedMavlinkMessageFieldPrimitiveType simple, string invalid)
	{
		if (string.Equals(invalid, "NaN", StringComparison.OrdinalIgnoreCase))
		{
			return _cache.GetOrAdd($"NaN:{simple.ConvertedType}", _ => new NaNInvalidFieldHandler(simple.ConvertedType));
		}

		if (invalid.IndexOf("MAX", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return _cache.GetOrAdd(invalid, _ => new MaxValueInvalidFieldHandler(invalid));
		}

		return _cache.GetOrAdd(invalid, _ => new LiteralInvalidFieldHandler(invalid));
	}

	private static IInvalidFieldHandler CreateForEnumType(GeneratedMavlinkMessageFieldEnumType enumType, string invalid)
	{
		string key = $"{invalid}:{enumType.GeneratedEnum.GeneratedName}";

		if (invalid.IndexOf("MAX", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return _cache.GetOrAdd(invalid, _ => new MaxValueInvalidFieldHandler(invalid));
		}

		return _cache.GetOrAdd(key, _ => new EnumInvalidFieldHandler(invalid, enumType.GeneratedEnum.GeneratedName, enumType.ConvertedType));
	}
}
