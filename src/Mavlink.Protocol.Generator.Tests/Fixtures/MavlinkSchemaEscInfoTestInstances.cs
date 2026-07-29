using System.Collections.ObjectModel;
using System.Globalization;

namespace Mavlink.Protocol.Generator.Tests;

public static class MavlinkSchemaEscInfoTestInstances
{
	private static Enum? _escFailureFlagsEnum;
	private static Enum? _escConnectionTypeEnum;
	private static Message? _escInfoMessage;

	/// <summary>
	/// Creates an Enum object representing ESC failure flags.
	/// <code>
	/// <![CDATA[
	/// <enum name="ESC_FAILURE_FLAGS" bitmask="true">
	///   <description>Flags to report ESC failures.</description>
	///   <entry value="0" name="ESC_FAILURE_NONE">
	///     <description>No ESC failure.</description>
	///   </entry>
	///   <entry value="1" name="ESC_FAILURE_OVER_CURRENT">
	///     <description>Over current failure.</description>
	///   </entry>
	///   <entry value="2" name="ESC_FAILURE_OVER_VOLTAGE">
	///     <description>Over voltage failure.</description>
	///   </entry>
	///   <entry value="4" name="ESC_FAILURE_OVER_TEMPERATURE">
	///     <description>Over temperature failure.</description>
	///   </entry>
	///   <entry value="8" name="ESC_FAILURE_OVER_RPM">
	///     <description>Over RPM failure.</description>
	///   </entry>
	///   <entry value="16" name="ESC_FAILURE_INCONSISTENT_CMD">
	///     <description>Inconsistent command failure i.e. out of bounds.</description>
	///   </entry>
	///   <entry value="32" name="ESC_FAILURE_MOTOR_STUCK">
	///     <description>Motor stuck failure.</description>
	///   </entry>
	///   <entry value="64" name="ESC_FAILURE_GENERIC">
	///     <description>Generic ESC failure.</description>
	///   </entry>
	/// </enum>
	/// ]]>
	/// </code>
	/// </summary>
	public static Enum CreateEscFailureFlagsEnum()
	{
		if (_escFailureFlagsEnum == null)
		{
			var entries = new Collection<Entry>
			{
				CreateEntry("0", "ESC_FAILURE_NONE", "No ESC failure."),
				CreateEntry("1", "ESC_FAILURE_OVER_CURRENT", "Over current failure."),
				CreateEntry("2", "ESC_FAILURE_OVER_VOLTAGE", "Over voltage failure."),
				CreateEntry("4", "ESC_FAILURE_OVER_TEMPERATURE", "Over temperature failure."),
				CreateEntry("8", "ESC_FAILURE_OVER_RPM", "Over RPM failure."),
				CreateEntry("16", "ESC_FAILURE_INCONSISTENT_CMD", "Inconsistent command failure i.e. out of bounds."),
				CreateEntry("32", "ESC_FAILURE_MOTOR_STUCK", "Motor stuck failure."),
				CreateEntry("64", "ESC_FAILURE_GENERIC", "Generic ESC failure.")
			};

			_escFailureFlagsEnum = new Enum("ESC_FAILURE_FLAGS", true, "Flags to report ESC failures.", entries);
		}
		return _escFailureFlagsEnum;
	}

	/// <summary>
	/// Creates an Enum object representing ESC connection types.
	/// <code>
	/// <![CDATA[
	/// <enum name="ESC_CONNECTION_TYPE">
	///   <description>Indicates the ESC connection type.</description>
	///   <entry value="0" name="ESC_CONNECTION_TYPE_PPM">
	///     <description>Traditional PPM ESC.</description>
	///   </entry>
	///   <entry value="1" name="ESC_CONNECTION_TYPE_SERIAL">
	///     <description>Serial Bus connected ESC.</description>
	///   </entry>
	///   <entry value="2" name="ESC_CONNECTION_TYPE_ONESHOT">
	///     <description>One Shot PPM ESC.</description>
	///   </entry>
	///   <entry value="3" name="ESC_CONNECTION_TYPE_I2C">
	///     <description>I2C ESC.</description>
	///   </entry>
	///   <entry value="4" name="ESC_CONNECTION_TYPE_CAN">
	///     <description>CAN-Bus ESC.</description>
	///   </entry>
	///   <entry value="5" name="ESC_CONNECTION_TYPE_DSHOT">
	///     <description>DShot ESC.</description>
	///   </entry>
	/// </enum>
	/// ]]>
	/// </code>
	/// </summary>
	public static Enum CreateEscConnectionTypeEnum()
	{
		if (_escConnectionTypeEnum == null)
		{
			var entries = new Collection<Entry>
			{
				CreateEntry("0", "ESC_CONNECTION_TYPE_PPM", "Traditional PPM ESC."),
				CreateEntry("1", "ESC_CONNECTION_TYPE_SERIAL", "Serial Bus connected ESC."),
				CreateEntry("2", "ESC_CONNECTION_TYPE_ONESHOT", "One Shot PPM ESC."),
				CreateEntry("3", "ESC_CONNECTION_TYPE_I2C", "I2C ESC."),
				CreateEntry("4", "ESC_CONNECTION_TYPE_CAN", "CAN-Bus ESC."),
				CreateEntry("5", "ESC_CONNECTION_TYPE_DSHOT", "DShot ESC.")
			};

			_escConnectionTypeEnum = new Enum("ESC_CONNECTION_TYPE", false, "Indicates the ESC connection type.", entries);
		}
		return _escConnectionTypeEnum;
	}

	/// <summary>
	/// Creates a Message object representing ESC information.
	/// <code>
	/// <![CDATA[
	/// <message id="290" name="ESC_INFO">
	///   <description>ESC information for lower rate streaming. Recommended streaming rate 1Hz. See ESC_STATUS for higher-rate ESC data.</description>
	///   <field type="uint8_t" name="index" instance="true">Index of the first ESC in this message. minValue = 0, maxValue = 60, increment = 4.</field>
	///   <field type="uint64_t" name="time_usec" units="us">Timestamp (UNIX Epoch time or time since system boot). The receiving end can infer timestamp format (since 1.1.1970 or since system boot) by checking for the magnitude the number.</field>
	///   <field type="uint16_t" name="counter">Counter of data packets received.</field>
	///   <field type="uint8_t" name="count">Total number of ESCs in all messages of this type. Message fields with an index higher than this should be ignored because they contain invalid data.</field>
	///   <field type="uint8_t" name="connection_type" enum="ESC_CONNECTION_TYPE">Connection type protocol for all ESC.</field>
	///   <field type="uint8_t" name="info" display="bitmask">Information regarding online/offline status of each ESC.</field>
	///   <field type="uint16_t[4]" name="failure_flags" enum="ESC_FAILURE_FLAGS" display="bitmask">Bitmap of ESC failure flags.</field>
	///   <field type="uint32_t[4]" name="error_count">Number of reported errors by each ESC since boot.</field>
	///   <field type="int16_t[4]" name="temperature" units="cdegC" invalid="[INT16_MAX]">Temperature of each ESC. INT16_MAX: if data not supplied by ESC.</field>
	/// </message>
	/// ]]>
	/// </code>
	/// </summary>
	public static Message CreateEscInfoMessage()
	{
		if (_escInfoMessage == null)
		{
			var fields = new Collection<Field>
			{
				CreateField("uint8_t", "index", true, "Index of the first ESC in this message. minValue = 0, maxValue = 60, increment = 4.", "us", "0", "60", "4"),
				CreateField("uint64_t", "time_usec", false, "Timestamp (UNIX Epoch time or time since system boot). The receiving end can infer timestamp format (since 1.1.1970 or since system boot) by checking for the magnitude the number."),
				CreateField("uint16_t", "counter", false, "Counter of data packets received."),
				CreateField("uint8_t", "count", false, "Total number of ESCs in all messages of this type. Message fields with an index higher than this should be ignored because they contain invalid data."),
				CreateField("uint8_t", "connection_type", false, "Connection type protocol for all ESC.", "ESC_CONNECTION_TYPE"),
				CreateField("uint8_t", "info", false, "Information regarding online/offline status of each ESC.", "bitmask"),
				CreateField("uint16_t[4]", "failure_flags", false, "Bitmap of ESC failure flags.", "ESC_FAILURE_FLAGS", "bitmask"),
				CreateField("uint32_t[4]", "error_count", false, "Number of reported errors by each ESC since boot."),
				CreateField("int16_t[4]", "temperature", false, "Temperature of each ESC. INT16_MAX: if data not supplied by ESC.", "cdegC", null, null, null, null, "[INT16_MAX]")
			};

			_escInfoMessage = new Message(290, "ESC_INFO", "ESC information for lower rate streaming. Recommended streaming rate 1Hz. See ESC_STATUS for higher-rate ESC data.", fields);
		}
		return _escInfoMessage;
	}

	private static Entry CreateEntry(string value, string name, string description)
	{
		return new Entry
		{
			Value = value,
			Name = name,
			Description = description
		};
	}

	private static Field CreateField(string type, string name, bool instance, string description, string? units = null, string? minValue = null, string? maxValue = null, string? enumType = null, string? display = null, string? invalid = null)
	{
		var field = new Field
		{
			Type = type,
			Name = name,
			Instance = instance,
			Description = description
		};
		if (units != null)
			field.Units = System.Enum.Parse<SiUnit>(units, true);
		if (minValue != null)
			field.MinValue = float.Parse(minValue, CultureInfo.InvariantCulture);
		if (maxValue != null)
			field.MaxValue = float.Parse(maxValue, CultureInfo.InvariantCulture);
		if (enumType != null)
			field.Enum = enumType;
		if (display != null)
			field.Display = display;
		if (invalid != null)
			field.Invalid = invalid;
		return field;
	}
}
