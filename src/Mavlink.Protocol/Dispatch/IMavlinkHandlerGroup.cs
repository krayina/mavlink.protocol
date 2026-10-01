namespace Mavlink;

internal interface IMavlinkHandlerGroup
{
	void Invoke(in MavlinkReceivedPacket packet, MavlinkEventBus bus);

	bool Remove(long token);
}
