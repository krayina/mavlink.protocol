namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a validation rule derived from a MAVLink field's 'invalid' attribute.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that no special invalid-value validation is applied.
/// </summary>
public sealed record GeneratedMavlinkMessageNoValidationRule() : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that the entire field (scalar or whole array) is considered invalid if it matches a specific raw value.
/// Corresponds to invalid="value" for scalars and invalid="[value:]" for arrays.
/// </summary>
public sealed record GeneratedMavlinkMessageWholeFieldValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that individual array elements are considered invalid if they match a specific raw value.
/// Corresponds to invalid="[value]".
/// </summary>
public sealed record GeneratedMavlinkMessagePerElementValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;
