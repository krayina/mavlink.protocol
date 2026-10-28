using System.Globalization;

namespace Mavlink.Protocol.Generator.Parsing;

/// <summary>
/// Element types a MAVLink field can have on the wire.
/// </summary>
internal enum WireType
{
	Char,
	Int8,
	UInt8,
	Int16,
	UInt16,
	Int32,
	UInt32,
	Int64,
	UInt64,
	Float,
	Double,
}

/// <summary>
/// A field's <c>type</c> attribute: element type, array length (0 for a scalar) and whether
/// it is the special <c>uint8_t_mavlink_version</c>.
/// </summary>
internal readonly record struct FieldType(WireType Element, int ArrayLength, bool IsMavlinkVersion)
{
	/// <summary>
	/// Longest array the generated code supports (<c>FixedArray1</c>…<c>FixedArray255</c>).
	/// </summary>
	public const int MaxArrayLength = 255;

	public bool IsArray => ArrayLength > 0;

	/// <summary>
	/// Size of one element in bytes.
	/// </summary>
	public int ElementSize => SizeOf(Element);

	/// <summary>
	/// Size of the whole field in the payload.
	/// </summary>
	public int WireLength => ElementSize * Math.Max(ArrayLength, 1);

	/// <summary>
	/// Element type name as it goes into CRC_EXTRA; <c>uint8_t_mavlink_version</c> counts as <c>uint8_t</c>.
	/// </summary>
	public string ElementName => NameOf(Element);

	/// <summary>
	/// Parses the type the way the MAVLink schema writes it: <c>uint16_t</c>, <c>float[4]</c>,
	/// <c>char[50]</c>, <c>uint8_t_mavlink_version</c>.
	/// </summary>
	/// <param name="error">Why the text is not a type; null on success.</param>
	public static bool TryParse(string text, out FieldType type, out string? error)
	{
		type = default;

		if (text == "uint8_t_mavlink_version")
		{
			type = new FieldType(WireType.UInt8, 0, IsMavlinkVersion: true);
			error = null;
			return true;
		}

		var elementText = text;
		var arrayLength = 0;
		var bracket = text.IndexOf('[');
		if (bracket >= 0)
		{
			if (!text.EndsWith("]", StringComparison.Ordinal)
				|| !int.TryParse(text.Substring(bracket + 1, text.Length - bracket - 2), NumberStyles.None, CultureInfo.InvariantCulture, out arrayLength))
			{
				error = $"'{text}' is not a valid array type; expected e.g. 'uint8_t[16]'.";
				return false;
			}

			if (arrayLength < 1 || arrayLength > MaxArrayLength)
			{
				error = $"Array length {arrayLength} in '{text}' must be from 1 to {MaxArrayLength}.";
				return false;
			}

			elementText = text.Substring(0, bracket);
		}

		if (!TryParseElement(elementText, out var element))
		{
			error = $"Unknown field type '{elementText}'.";
			return false;
		}

		type = new FieldType(element, arrayLength, IsMavlinkVersion: false);
		error = null;
		return true;
	}

	public override string ToString()
		=> IsMavlinkVersion ? "uint8_t_mavlink_version"
		: IsArray ? $"{ElementName}[{ArrayLength.ToString(CultureInfo.InvariantCulture)}]"
		: ElementName;

	private static bool TryParseElement(string text, out WireType element)
	{
		switch (text)
		{
			case "char": element = WireType.Char; return true;
			case "int8_t": element = WireType.Int8; return true;
			case "uint8_t": element = WireType.UInt8; return true;
			case "int16_t": element = WireType.Int16; return true;
			case "uint16_t": element = WireType.UInt16; return true;
			case "int32_t": element = WireType.Int32; return true;
			case "uint32_t": element = WireType.UInt32; return true;
			case "int64_t": element = WireType.Int64; return true;
			case "uint64_t": element = WireType.UInt64; return true;
			case "float": element = WireType.Float; return true;
			case "double": element = WireType.Double; return true;
			default: element = default; return false;
		}
	}

	private static int SizeOf(WireType element) => element switch
	{
		WireType.Char or WireType.Int8 or WireType.UInt8 => 1,
		WireType.Int16 or WireType.UInt16 => 2,
		WireType.Int32 or WireType.UInt32 or WireType.Float => 4,
		WireType.Int64 or WireType.UInt64 or WireType.Double => 8,
		_ => throw new ArgumentOutOfRangeException(nameof(element), element, null),
	};

	private static string NameOf(WireType element) => element switch
	{
		WireType.Char => "char",
		WireType.Int8 => "int8_t",
		WireType.UInt8 => "uint8_t",
		WireType.Int16 => "int16_t",
		WireType.UInt16 => "uint16_t",
		WireType.Int32 => "int32_t",
		WireType.UInt32 => "uint32_t",
		WireType.Int64 => "int64_t",
		WireType.UInt64 => "uint64_t",
		WireType.Float => "float",
		WireType.Double => "double",
		_ => throw new ArgumentOutOfRangeException(nameof(element), element, null),
	};
}
