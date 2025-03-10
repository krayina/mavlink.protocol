using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a strategy for generating deserialization code for Mavlink messages.
/// </summary>
public interface IMavlinkDeserializationGeneratorStrategy
{
	/// <summary>
	/// Appends initialization code for the deserialization process.
	/// </summary>
	void AppendBufferInitialization(StringBuilder sb, string messageName, int requiredSize, string payloadParameterName);

	/// <summary>
	/// Appends deserialization code for a single field and returns the variable name for object initialization.
	/// </summary>
	string AppendFieldDeserialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName);

	/// <summary>
	/// Appends the return statement with the constructed message object.
	/// </summary>
	void AppendReturnStatement(StringBuilder sb, string messageName, IDictionary<GeneratedMavlinkMessageField, string> fields);
}
