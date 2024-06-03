using System.Runtime.CompilerServices;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Struct)]
public class MavlinkTypeAttribute : Attribute
{
	public string TypeName { get; }

	public MavlinkTypeAttribute([CallerMemberName] string? typeName = null)
	{
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
	}
}
