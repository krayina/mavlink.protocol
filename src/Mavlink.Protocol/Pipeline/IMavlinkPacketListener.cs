namespace Mavlink;

internal interface IMavlinkPacketListener
{
	void OnPacketReceived(in MavlinkReceivedPacket packet);
}

internal interface IMavlinkParserErrorListener
{
	void OnParserError(MavlinkPacketParseResult result);

	void OnReceiverFault(Exception exception);
}
