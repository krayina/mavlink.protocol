using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Represents a validation rule derived from a MAVLink field's <c>invalid</c> attribute.
/// <para>
/// Base for all specific invalid-value rule definitions. Stores raw values exactly as they appear
/// in the MAVLink XML and contains no C# logic.
/// </para>
/// </summary>
public abstract record GeneratedMavlinkMessageFieldValidationRuleDefinition;

/// <summary>
/// Represents the absence of any invalid-value rule.
/// <para>
/// Produced when the MAVLink field does not have an <c>invalid</c> attribute.
/// </para>
/// </summary>
/// <example>
/// XML:
/// <code>
/// &lt;field type="uint32_t" name="time_boot_ms"&gt;Timestamp since boot&lt;/field&gt;
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessageNoValidationRuleDefinition()
	: GeneratedMavlinkMessageFieldValidationRuleDefinition;

/// <summary>
/// A rule where the entire field is considered invalid if it matches a specific value.
/// <para>
/// Applies to scalars, and to arrays when the first element acts as a sentinel (the <c>[X:]</c> form).
/// </para>
/// </summary>
/// <example>
/// Scalar XML:
/// <code>
/// &lt;field type="int16_t" name="current_battery" invalid="-1"&gt;...&lt;/field&gt;
/// </code>
/// </example>
/// <example>
/// Array sentinel XML:
/// <code>
/// &lt;field type="float[6]" name="covariance" invalid="[NaN:]"&gt;...&lt;/field&gt;
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessageWholeFieldValidationRuleDefinition(string RawInvalidValue)
	: GeneratedMavlinkMessageFieldValidationRuleDefinition;

/// <summary>
/// A rule where each element of an array is considered invalid if it matches a specific value.
/// </summary>
/// <example>
/// XML:
/// <code>
/// &lt;field type="float[21]" name="vel_variance" invalid="[NaN]"&gt;...&lt;/field&gt;
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessagePerElementValidationRuleDefinition(string RawInvalidValue)
	: GeneratedMavlinkMessageFieldValidationRuleDefinition;

/// <summary>
/// A rule where specific indices in an array have their own invalid values.
/// <para>
/// Use <c>null</c> or empty to mean "no rule at this index".
/// </para>
/// </summary>
/// <example>
/// XML:
/// <code>
/// &lt;field type="uint16_t[4]" name="voltages" invalid="[65535,,65535,]"&gt;...&lt;/field&gt;
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessagePerIndexValidationRuleDefinition(
	ImmutableArray<string?> InvalidValuesInOrder)
	: GeneratedMavlinkMessageFieldValidationRuleDefinition;
