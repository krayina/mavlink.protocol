namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete generator for creating serialization methods that operate on Span&lt;byte&gt;.
/// </summary>
public class MavlinkMessageSpanSerializationMethodGenerator : MavlinkMessageSerializationMethodGenerator
{
	public MavlinkMessageSpanSerializationMethodGenerator(IInvalidValueExpressionBuilder invalidValueBuilder, bool useObjectiveBitmask = true)
		: base(new SpanSerializationPayloadWriteScribanStrategy(), invalidValueBuilder, useObjectiveBitmask)
	{
	}

	/// <inheritdoc/>
	protected override string GetMethodSignature(string methodName, string messageName)
	{
		return $"public int {methodName}(System.Span<byte> span)";
	}

	/// <inheritdoc/>
	protected override string GetInitializationBlock(string messageName, int requiredSize)
	{
		return $@"
if (span.Length < {requiredSize})
{{
    throw new System.ArgumentException(""Span is too small for this message."", nameof(span));
}}";
	}
}
