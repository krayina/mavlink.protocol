using System.ComponentModel;

namespace Mavlink.Dialects.Infrastructure;

/// <summary>
/// Emitted on every generated enum: the enum's name in the dialect XML. The generator in a
/// referencing project uses it to map <c>enum="MAV_TYPE"</c> to the compiled C# enum
/// without needing the XML.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class MavlinkEnumAttribute : Attribute
{
	public MavlinkEnumAttribute(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Name of the enum in the dialect XML, e.g. "MAV_TYPE".
	/// </summary>
	public string Name { get; }
}
