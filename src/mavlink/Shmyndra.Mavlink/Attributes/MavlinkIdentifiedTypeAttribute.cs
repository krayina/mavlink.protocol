using System.Runtime.CompilerServices;

namespace MavlinkTypes;

[AttributeUsage(AttributeTargets.Struct)]
public class MavlinkIdentifiedTypeAttribute : MavlinkTypeAttribute
{
	public uint Id { get; }

	public MavlinkIdentifiedTypeAttribute(uint id, string xmlName, [CallerMemberName] string? typeName = null)
		: base(xmlName, typeName)
	{
		Id = id;
	}
}
