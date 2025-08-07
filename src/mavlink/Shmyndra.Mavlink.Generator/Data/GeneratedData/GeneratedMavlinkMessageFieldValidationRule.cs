namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a validation rule derived from a MAVLink field's metadata.
/// </summary>
public abstract record GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that no special validation is applied.
/// </summary>
public sealed record GeneratedMavlinkMessageNoValidationRule() : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that validation should be applied to the entire field value (e.g., a scalar or an entire array).
/// </summary>
public sealed record GeneratedMavlinkMessageWholeFieldValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;

/// <summary>
/// Indicates that validation should be applied to each element of an array field.
/// </summary>
public sealed record GeneratedMavlinkMessagePerElementValidationRule(string RawInvalidValue) : GeneratedMavlinkMessageFieldValidationRule;
