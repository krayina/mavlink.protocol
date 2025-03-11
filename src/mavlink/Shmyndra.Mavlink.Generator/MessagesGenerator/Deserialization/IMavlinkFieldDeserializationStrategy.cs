using System.Text;

namespace Shmyndra.Mavlink.Generator;

public interface IMavlinkFieldDeserializationStrategy
{
	string DeserializeField(StringBuilder sb, GeneratedMavlinkMessageField field, ref int offset, string currentNamespace, string payloadParameterName);
}
