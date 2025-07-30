using System.Collections.Concurrent;

namespace Shmyndra.Mavlink.Generator;

public static class InvalidFieldHandlerFactory
{
	private static readonly ConcurrentDictionary<string, IValidationConditionProvider> _cache =
		new ConcurrentDictionary<string, IValidationConditionProvider>(StringComparer.OrdinalIgnoreCase);

	public static IValidationConditionProvider? Create(GeneratedMavlinkMessageField field)
	{
		if (string.IsNullOrWhiteSpace(field.Original.Invalid))
		{
			return null;
		}

		string invalid = field.Original.Invalid!;

		switch (field.GeneratedType)
		{
			case GeneratedMavlinkMessageFieldArrayType arrayType when invalid.StartsWith("["):
				{
					string trimmedInvalid = invalid.Trim('[', ']');
					if (trimmedInvalid.Contains(',') || trimmedInvalid.Contains(':'))
					{
						return _cache.GetOrAdd(invalid, _ => new ArrayInvalidFieldHandler(invalid, arrayType.ArrayLength));
					}
					else
					{
						var elementType = new GeneratedMavlinkMessageFieldPrimitiveType(arrayType.ConvertedType, arrayType.Original);
						return CreateForSimpleType(elementType, trimmedInvalid);
					}
				}
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType when invalid.StartsWith("["):
				{
					string trimmedInvalid = invalid.Trim('[', ']');
					if (trimmedInvalid.Contains(',') || trimmedInvalid.Contains(':'))
					{
						return _cache.GetOrAdd(invalid, _ => new ArrayInvalidFieldHandler(invalid, arrayEnumType.ArrayLength));
					}
					else
					{
						var elementType = new GeneratedMavlinkMessageFieldEnumType(arrayEnumType.ConvertedType, arrayEnumType.GeneratedEnum, arrayEnumType.Original);
						return CreateForEnumType(elementType, trimmedInvalid);
					}
				}
			case GeneratedMavlinkMessageFieldEnumType enumType:
				return CreateForEnumType(enumType, invalid);

			case GeneratedMavlinkMessageFieldPrimitiveType simpleType:
				return CreateForSimpleType(simpleType, invalid);

			default:
				throw new NotSupportedException($"Invalid condition '{invalid}' is not supported for type {field.GeneratedType}");
		}
	}

	private static IValidationConditionProvider CreateForSimpleType(GeneratedMavlinkMessageFieldPrimitiveType simple, string invalid)
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

	private static IValidationConditionProvider CreateForEnumType(GeneratedMavlinkMessageFieldEnumType enumType, string invalid)
	{
		string key = $"{invalid}:{enumType.GeneratedEnum.GeneratedName}";

		if (invalid.IndexOf("MAX", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return _cache.GetOrAdd(invalid, _ => new MaxValueInvalidFieldHandler(invalid));
		}

		return _cache.GetOrAdd(key, _ => new EnumInvalidFieldHandler(invalid, enumType.GeneratedEnum.GeneratedName, enumType.ConvertedType));
	}
}
