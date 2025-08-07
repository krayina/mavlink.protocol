using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a validation rule derived from a MAVLink field's 'invalid' attribute.
/// This abstract record serves as the base for all specific invalid-value validation rules.
/// The MAVLink specification allows fields to have a special value indicating that their data is invalid
/// and should be ignored by the recipient.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// <para>
/// Represents the absence of a specific invalid-value rule.
/// </para>
/// <para>
/// This is used for fields that do not have an 'invalid' attribute in the MAVLink XML definition.
/// </para>
/// <para><b>Example XML:</b></para>
/// <code>
/// &lt;field type="uint32_t" name="time_boot_ms"&gt;Timestamp (milliseconds since system boot).&lt;/field&gt;
/// </code>
/// </summary>
public sealed record GeneratedMavlinkMessageNoValidationRule() : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// <para>
/// Represents a rule where the entire field is considered invalid if it matches a specific value.
/// </para>
/// <para>
/// This applies to both scalar fields and array fields where the entire array is invalidated by a marker in the first element.
/// </para>
/// <para><b>Example for a scalar field:</b></para>
/// <code>
/// &lt;!-- XML Definition --&gt;
/// &lt;field type="int16_t" name="current_battery" invalid="-1"&gt;Battery current...&lt;/field&gt;
/// 
/// &lt;!-- C# Representation --&gt;
/// new GeneratedMavlinkMessageWholeFieldValidationRule("-1")
/// </code>
/// <para><b>Example for an array field:</b></para>
/// <code>
/// &lt;!-- XML Definition (invalidated by the first element) --&gt;
/// &lt;field type="float[6]" name="covariance" invalid="[NaN:]"&gt;Covariance matrix...&lt;/field&gt;
/// 
/// &lt;!-- C# Representation --&gt;
/// new GeneratedMavlinkMessageWholeFieldValidationRule("NaN")
/// </code>
/// </summary>
/// <param name="RawInvalidValue">The raw string representation of the invalid value (e.g., "NaN", "-1", "UINT16_MAX").</param>
public sealed record GeneratedMavlinkMessageWholeFieldValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// <para>
/// Represents a rule where individual elements of an array are considered invalid if they match a specific value.
/// </para>
/// <para>
/// This rule applies uniformly to all elements in the array.
/// </para>
/// <para><b>Example:</b></para>
/// <code>
/// &lt;!-- XML Definition --&gt;
/// &lt;field type="float[21]" name="vel_variance" invalid="[NaN]"&gt;Velocity variance&lt;/field&gt;
/// 
/// &lt;!-- C# Representation --&gt;
/// new GeneratedMavlinkMessagePerElementValidationRule("NaN")
/// </code>
/// </summary>
/// <param name="RawInvalidValue">The raw string representation of the value that makes an element invalid (e.g., "NaN", "UINT32_MAX").</param>
public sealed record GeneratedMavlinkMessagePerElementValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// <para>
/// Represents a complex rule where array elements are considered invalid based on their specific index and a corresponding value.
/// </para>
/// <para>
/// The list of invalid values directly corresponds to the comma-separated values in the 'invalid' attribute.
/// </para>
/// <para><b>Example with a value for the first and third element:</b></para>
/// <code>
/// &lt;!-- XML Definition --&gt;
/// &lt;field type="uint16_t[4]" name="voltages" invalid="[65535,,65535,]"&gt;Battery voltages...&lt;/field&gt;
/// 
/// &lt;!-- C# Representation --&gt;
/// new GeneratedMavlinkMessagePerIndexValidationRule(ImmutableList.Create("65535", "", "65535", ""))
/// </code>
/// </summary>
/// <param name="InvalidValuesInOrder">
/// An immutable list of raw string values. The index in the list corresponds to the element index in the MAVLink array.
/// An empty or null string in the list signifies that the element at that index does not have a specific invalid value rule.
/// </param>
public sealed record GeneratedMavlinkMessagePerIndexValidationRule(
	IImmutableList<string> InvalidValuesInOrder) : GeneratedMavlinkMessageFieldValidationRule;
