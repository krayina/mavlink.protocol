using System.Text;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Defines a strategy for generating serialization code for Mavlink messages.
/// Implementations of this interface provide methods to append initialization, field serialization,
/// and return statements to a StringBuilder, enabling flexible buffer-based or span-based serialization approaches.
/// </summary>
public interface IMavlinkSerializationGeneratorStrategy
{
	void AppendBufferInitialization(StringBuilder sb, int requiredSize);
	void AppendFieldSerialization(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace);
	void AppendReturnStatement(StringBuilder sb);
}
