namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents the semantic placement of the <c>Invalidatable&lt;T&gt;</c> wrapper
/// for a generated MAVLink message field property. This abstraction decouples
/// the property generator from the specific details of the validation expression.
/// </summary>
public abstract record InvalidatabilityPlacement;

/// <summary>
/// Indicates that the property should not be wrapped in <c>Invalidatable&lt;T&gt;</c>.
/// The field is always considered valid and will be generated with its base type.
/// </summary>
public sealed record NoInvalidatability() : InvalidatabilityPlacement;

/// <summary>
/// Indicates that the entire field (be it a scalar or an array) should be
/// wrapped in <c>Invalidatable&lt;T&gt;</c>.
/// </summary>
/// <example>
/// Generates: <c>Invalidatable&lt;int&gt;</c> or <c>Invalidatable&lt;ImmutableArray&lt;float&gt;&gt;</c>.
/// </example>
public sealed record WholeFieldInvalidatability() : InvalidatabilityPlacement;

/// <summary>
/// Indicates that each element of an array field should be wrapped in <c>Invalidatable&lt;T&gt;</c>.
/// </summary>
/// <example>
/// Generates: <c>ImmutableArray&lt;Invalidatable&lt;float&gt;&gt;</c>.
/// </example>
public sealed record PerElementInvalidatability() : InvalidatabilityPlacement;
