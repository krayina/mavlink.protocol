using System.Text;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkFieldSerializationStrategy
{
	void SerializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string variableName, string currentNamespace);
}
