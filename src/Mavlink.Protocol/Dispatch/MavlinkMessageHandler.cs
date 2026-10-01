namespace Mavlink;

public delegate void MavlinkMessageHandler<T>(T message, in MavlinkReceivedPacket packet)
	where T : struct, IMavlinkMessage;

public delegate void MavlinkMessageHandler(IMavlinkMessage message, in MavlinkReceivedPacket packet);

public delegate bool MavlinkPacketFilter(in MavlinkReceivedPacket packet);
