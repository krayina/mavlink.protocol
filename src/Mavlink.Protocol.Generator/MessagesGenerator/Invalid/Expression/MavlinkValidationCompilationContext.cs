namespace Mavlink.Protocol.Generator;

/// <summary>
/// Provides all necessary contextual information for a single MAVLink validation rule compilation operation.
///
/// <para>
/// This structure acts as a "Parameter Object", bundling together all dependencies required by a
/// <see cref="MavlinkMessageFieldValidationRuleCompiler"/>. This design simplifies the signature of the
/// operation's `Compile` method and decouples the operations from the main generator.
/// </para>
/// </summary>
/// <param name="FieldType">
/// Represents the C# type information for the MAVLink field being processed.
/// This is a critical piece of context, as it allows the compiler to generate a semantically
/// correct validation expression (e.g., knowing that "NaN" is only valid for floating-point types).
///
/// <para>
/// During the initial property generation phase, this object is created by analyzing the raw
/// MAVLink XML. For all subsequent phases (like deserialization), this object is retrieved directly
/// from the <c>GeneratedMavlinkMessageField.GeneratedType</c> property.
/// </para>
/// </param>
/// <param name="ExpressionBuilder">
/// A service that encapsulates the logic for building C# condition strings from raw MAVLink
/// string values.
/// </param>
public readonly record struct MavlinkValidationCompilationContext(
	GeneratedMavlinkMessageFieldType FieldType,
	IInvalidValueExpressionBuilder ExpressionBuilder);
