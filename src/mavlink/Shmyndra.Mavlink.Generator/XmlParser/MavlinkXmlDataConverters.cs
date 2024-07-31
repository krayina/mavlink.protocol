using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkXmlDataConverters
{
	public static MavlinkData ConvertToMavlinkData(this Mavlink mavlink, Func<IEnumerable<Enum>, IEnumerable<Enum>>? sortEnumsPredicate = null)
	{
		var enums = sortEnumsPredicate != null
			? sortEnumsPredicate(mavlink.Enums).Select(e => e.ConvertToMavlinkEnum()).ToImmutableArray()
			: mavlink.Enums.Select(e => e.ConvertToMavlinkEnum()).ToImmutableArray();

		var messages = mavlink.Messages.Select(m => m.ConvertToMavlinkMessage()).ToImmutableArray();
		var includes = mavlink.Include.ToImmutableArray();

		return new MavlinkData(enums, messages, includes, mavlink.Version, mavlink.Dialect);
	}

	public static MavlinkEnum ConvertToMavlinkEnum(this Enum @enum)
	{
		var entries = @enum.Entry.Select(e => e.ConvertToMavlinkEnumEntry()).ToImmutableArray();

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

	public static MavlinkMessage ConvertToMavlinkMessage(this Message message)
	{
		var fields = message.Field.Select(f => f.ConvertToMavlinkMessageField()).ToImmutableArray();

		return new MavlinkMessage(
			message.Id,
			message.Name,
			message.Description,
			fields,
			message.Deprecated?.ConvertToMavlinkDeprecatedInfo()
		);
	}

	public static MavlinkMessageField ConvertToMavlinkMessageField(this Field field)
	{
		return new MavlinkMessageField(
			field.Type.ConvertToMavlinkMessageFieldType(field.Enum),
			field.Name,
			field.Description,
			field.Display?.ConvertToMavlinkMessageFieldDisplay() ?? MavlinkMessageFieldDisplay.None,
			field.Units.ConvertToMavlinkSystemUnit(),
			field.IsRequired,
			field.PrintFormat,
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

	public static MavlinkMessageFieldType ConvertToMavlinkMessageFieldType(this string type, string? enumDependency)
	{
		if (enumDependency != null)
		{
			return new MavlinkMessageFieldEnumType(type, enumDependency);
		}
		else
		{
			return new MavlinkMessageFieldType(type);
		}
	}
}
