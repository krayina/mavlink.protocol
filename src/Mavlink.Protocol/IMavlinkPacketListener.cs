namespace Mavlink;

internal interface IMavlinkPacketListener
{
	void OnPacketReceived(in MavlinkReceivedPacket packet);
}

internal interface IMavlinkParserErrorListener
{
	void OnParserError(MavlinkDeserializeResult result);

	void OnReceiverFault(Exception exception);
}
