namespace Mavlink.Protocol.Generator;

/// <summary>
/// A concrete generator for creating serialization methods that operate on byte arrays (byte[]).
/// </summary>
public class MavlinkMessageBufferSerializationMethodGenerator : MavlinkMessageSerializationMethodGenerator
{
	public MavlinkMessageBufferSerializationMethodGenerator(IInvalidValueExpressionBuilder invalidValueBuilder, bool useObjectiveBitmask = true)
		: base(new BufferSerializationPayloadWriteScribanStrategy(), invalidValueBuilder, useObjectiveBitmask)
	{
	}

	/// <inheritdoc/>
	protected override string GetMethodSignature(string methodName, string messageName)
	{
		return $"public int {methodName}(byte[] buffer, int offset = 0)";
	}

	/// <inheritdoc/>
	protected override string GetInitializationBlock(string messageName, int requiredSize)
	{
		return $@"
if (buffer.Length - offset < {requiredSize})
{{
    throw new System.ArgumentException(""Buffer is too small for this message."", nameof(buffer));
}}";
	}
}
