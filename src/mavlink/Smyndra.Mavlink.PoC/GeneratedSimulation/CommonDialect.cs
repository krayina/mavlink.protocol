using Mavlink.Common;

namespace Mavlink;

internal static class CommonDialect
{
	public static readonly IReadOnlyList<IMavlinkMessageInfo> AllMessages = new List<IMavlinkMessageInfo>
	{
		HeartbeatMessageInfo.Instance,
        // ... SysStatusMessageInfo.Instance,
        // ... etc.
    }.AsReadOnly();

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
	[System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
	internal static void Initialize()
	{
		MavlinkDialectRegistry.Register(AllMessages);
	}
}
