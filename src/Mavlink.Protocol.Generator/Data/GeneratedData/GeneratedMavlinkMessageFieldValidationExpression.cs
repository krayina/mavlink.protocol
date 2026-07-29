using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a compiled C# validation expression for a generated MAVLink message field.
/// <para>
/// Produced from a corresponding
/// <see cref="GeneratedMavlinkMessageFieldValidationRuleDefinition"/>. Contains ready-to-use
/// boolean expressions in C# syntax.
/// </para>
/// </summary>
public abstract record GeneratedMavlinkMessageFieldValidationExpression;

/// <summary>
/// No validation logic — the field is always considered valid.
/// <para>
/// From rule:
/// <see cref="GeneratedMavlinkMessageNoValidationRuleDefinition"/>
/// </para>
/// </summary>
public sealed record GeneratedMavlinkMessageFieldNoValidationExpression()
	: GeneratedMavlinkMessageFieldValidationExpression;

/// <summary>
/// Validation expression that applies to the entire field.
/// <para>
/// From rule:
/// <see cref="GeneratedMavlinkMessageWholeFieldValidationRuleDefinition"/>
/// </para>
/// </summary>
/// <example>
/// Scalar C#:
/// <code>
/// value != -1
/// </code>
/// </example>
/// <example>
/// Array sentinel C#:
/// <code>
/// !float.IsNaN(first)
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessageFieldWholeValidationExpression(string ConditionForWholeField)
	: GeneratedMavlinkMessageFieldValidationExpression;

/// <summary>
/// Validation expression applied to each element of an array.
/// <para>
/// Uses <c>"{element}"</c> as a placeholder for the array element variable in generated code.
/// From rule:
/// <see cref="GeneratedMavlinkMessagePerElementValidationRuleDefinition"/>
/// </para>
/// </summary>
/// <example>
/// C# template:
/// <code>
/// !float.IsNaN({element})
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessageFieldPerElementValidationExpression(string ElementConditionTemplate)
	: GeneratedMavlinkMessageFieldValidationExpression;

/// <summary>
/// Validation expressions for specific indices in an array.
/// <para>
/// Keys are element indices; values are ready-to-use C# boolean expressions that evaluate to
/// <c>true</c> when the element is valid.
/// From rule:
/// <see cref="GeneratedMavlinkMessagePerIndexValidationRuleDefinition"/>
/// </para>
/// </summary>
/// <example>
/// C# mapping:
/// <code>
/// 0 =&gt; "element != 65535",
/// 2 =&gt; "element != 65535"
/// </code>
/// </example>
public sealed record GeneratedMavlinkMessageFieldPerIndexValidationExpression(
	ImmutableDictionary<int, string> ConditionByIndex)
	: GeneratedMavlinkMessageFieldValidationExpression;
