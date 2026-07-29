namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A concrete generator for creating deserialization methods that operate on byte arrays (byte[]).
/// </summary>
public class MavlinkMessageBufferDeserializationMethodGenerator : MavlinkMessageDeserializationMethodGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageBufferDeserializationMethodGenerator"/> class.
	/// </summary>
	public MavlinkMessageBufferDeserializationMethodGenerator(
		IMavlinkMessageFieldValidationExpressionCompiler validationCompiler,
		bool useObjectiveBitmask = true)
		: base(validationCompiler, new BufferDeserializationPayloadReadScribanStrategy(), useObjectiveBitmask)
	{
	}

	/// <inheritdoc/>
	protected override string GetMethodSignature(string methodName, string messageName)
	{
		return $"public static {messageName} {methodName}(byte[] payload)";
	}

	/// <inheritdoc/>
	protected override string GetInitializationBlock(string messageName, int requiredSize)
	{
		return $@"
if (payload == null || payload.Length == 0)
{{
    return new {messageName}();
}}
if (payload.Length < {requiredSize})
{{
    var paddedPayload = new byte[{requiredSize}];
    System.Array.Copy(payload, paddedPayload, payload.Length);
    payload = paddedPayload;
}}";
	}
}
