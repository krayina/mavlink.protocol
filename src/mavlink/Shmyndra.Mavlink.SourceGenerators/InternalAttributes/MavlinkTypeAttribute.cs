using System.Runtime.CompilerServices;

namespace MavlinkTypes;

/// <summary>
/// This file must be linked from foreign folder
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Struct)]
internal class MavlinkTypeAttribute : Attribute
{
	public string TypeName { get; }
	public string XmlName { get; }

	public MavlinkTypeAttribute(string xmlName, [CallerMemberName] string? typeName = null)
	{
		TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
		XmlName = xmlName ?? throw new ArgumentNullException(nameof(xmlName));
	}
}
