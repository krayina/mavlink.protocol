using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkXmlDataConverters
{
	private static readonly Dictionary<string, (string TypeName, int Size)> _typeMap = new()
	{
		{ "char", ("char", 1) },
		{ "uint8_t", ("byte", 1) },
		{ "int8_t", ("sbyte", 1) },
		{ "uint16_t", ("ushort", 2) },
		{ "int16_t", ("short", 2) },
		{ "uint32_t", ("uint", 4) },
		{ "int32_t", ("int", 4) },
		{ "uint64_t", ("ulong", 8) },
		{ "int64_t", ("long", 8) },
		{ "float", ("float", 4) },
		{ "double", ("double", 8) },
		{ "uint8_t_mavlink_version", ("byte", 1) }
	};

	public static MavlinkData ConvertToMavlinkData(this Mavlink mavlink, Func<string, bool> isFieldRequired)
	{
		var enums = mavlink.Enums.Select(e => e.ConvertToMavlinkEnum()).ToImmutableArray();
		var messages = mavlink.Messages.Select(m => m.ConvertToMavlinkMessage(isFieldRequired)).ToImmutableArray();
		var includes = mavlink.Include.ToImmutableArray();

		return new MavlinkData(enums, messages, includes, mavlink.Version, mavlink.Dialect);
	}

	public static MavlinkEnum ConvertToMavlinkEnum(this Enum @enum)
	{
		var entries = @enum.Entry.Select(e => e.ConvertToMavlinkEnumEntry()).ToImmutableList();

		return new MavlinkEnum(
			@enum.Name,
			@enum.Description,
			@enum.Bitmask,
			entries,
			@enum.Deprecated?.ConvertToMavlinkDeprecatedInfo()
		);
	}

	public static MavlinkEnumEntry ConvertToMavlinkEnumEntry(this Entry entry)
	{
		var details = entry.Param.Select(p => p.ConvertToMavlinkEnumEntryDetail()).ToImmutableArray();

		return new MavlinkEnumEntry(
			entry.Name,
			uint.Parse(entry.Value),
			entry.Description,
			details,
			entry.Deprecated?.ConvertToMavlinkDeprecatedInfo(),
			entry.HasLocation,
			entry.IsDestination,
			entry.MissionOnly
		);
	}

	public static MavlinkEnumEntryDetail ConvertToMavlinkEnumEntryDetail(this Param param)
	{
		return new MavlinkEnumEntryDetail(
			param.Index,
			param.Label,
			param.Units.ConvertToMavlinkSystemUnit(),
			param.Instance,
			param.Enum,
			param.DecimalPlacesSpecified ? param.DecimalPlaces : (byte?)null,
			param.IncrementSpecified ? param.Increment : (float?)null,
			param.MinValueSpecified ? param.MinValue : (float?)null,
			param.MaxValueSpecified ? param.MaxValue : (float?)null,
			param.ReservedSpecified ? param.Reserved : (bool?)null,
			param.Default,
			param.Text
		);
	}

	public static MavlinkMessage ConvertToMavlinkMessage(this Message message, Func<string, bool> isFieldRequired)
	{
		var fields = message.Field.Select(f => f.ConvertToMavlinkMessageField(isFieldRequired)).ToImmutableList();

		return new MavlinkMessage(
			message.Id,
			message.Name,
			message.Description,
			fields,
			message.Deprecated?.ConvertToMavlinkDeprecatedInfo()
		);
	}

	public static MavlinkMessageField ConvertToMavlinkMessageField(this Field field, Func<string, bool> isRequired)
	{
		return new MavlinkMessageField(
			field.Type.ConvertToFieldType(),
			field.Name,
			field.Description,
			field.Display?.ConvertToMavlinkMessageFieldDisplay() ?? MavlinkMessageFieldDisplay.None,
			field.Units.ConvertToMavlinkSystemUnit(),
			isRequired.Invoke(field.Name),
			field.PrintFormat,
			field.Enum,
			field.IncrementSpecified ? field.Increment : null,
			field.MinValueSpecified ? field.MinValue : null,
			field.MaxValueSpecified ? field.MaxValue : null,
			field.Instance,
			field.Default,
			field.Invalid
		);
	}

	public static MavlinkDeprecatedInfo ConvertToMavlinkDeprecatedInfo(this Deprecated deprecated)
	{
		return new MavlinkDeprecatedInfo(
			deprecated.Description,
			deprecated.Since,
			deprecated.ReplacedBy,
			deprecated.Text
		);
	}

	public static MavlinkMessageFieldDisplay ConvertToMavlinkMessageFieldDisplay(this string display)
	{
		return System.Enum.TryParse(display, true, out MavlinkMessageFieldDisplay result) ? result : MavlinkMessageFieldDisplay.None;
	}

	public static MavlinkSystemUnit ConvertToMavlinkSystemUnit(this SiUnit siUnit)
	{
		return System.Enum.TryParse(siUnit.ToString(), true, out MavlinkSystemUnit result) ? result : MavlinkSystemUnit.Empty;
	}

	public static FieldType ConvertToFieldType(this string type)
	{
		// Example implementation: additional logic needed to handle array and enum types properly
		return new FieldType(type);
	}
}
