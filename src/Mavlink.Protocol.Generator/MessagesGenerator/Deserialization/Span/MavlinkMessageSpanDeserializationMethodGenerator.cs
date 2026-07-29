namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete generator for creating deserialization methods that operate on ReadOnlySpan&lt;byte&gt;.
/// </summary>
public class MavlinkMessageSpanDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageSpanDeserializationMethodGenerator"/> class.
	/// </summary>
	public MavlinkMessageSpanDeserializationMethodGenerator(
		IMavlinkMessageFieldValidationExpressionCompiler validationCompiler,
		bool useObjectiveBitmask = true)
		: base(validationCompiler, new SpanDeserializationPayloadReadScribanStrategy(), useObjectiveBitmask)
	{
	}

	/// <inheritdoc/>
	protected override string GetMethodSignature(string methodName, string messageName)
	{
		return $"public static {messageName} {methodName}(System.ReadOnlySpan<byte> payload)";
	}

	/// <inheritdoc/>
	protected override string GetInitializationBlock(string messageName, int requiredSize)
	{
		return $@"
if (payload.IsEmpty)
{{
    return new {messageName}();
}}
System.ReadOnlySpan<byte> span = payload;
if (payload.Length < {requiredSize})
{{
    byte[] paddedBuffer = new byte[{requiredSize}];
    payload.CopyTo(paddedBuffer);
    span = paddedBuffer;
}}";
	}
}
