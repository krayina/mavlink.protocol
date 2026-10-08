namespace Mavlink;

internal static class MavlinkFrameWriter
{
	public static int Write<T>(
		in T message,
		IMavlinkMessageInfo info,
		byte seq,
		byte systemId,
		byte componentId,
		byte[] buffer,
		MavlinkPacketVersion version,
		MavlinkSigner? signer = null)
		where T : IMavlinkMessage
	{
		if (version == MavlinkPacketVersion.V1)
		{
			return info is IMavlinkMessageInfo<T> typed
				? MavlinkV1FrameWriter.Write(in message, typed, seq, systemId, componentId, buffer)
				: MavlinkV1FrameWriter.Write(message, info, seq, systemId, componentId, buffer);
		}
		else
		{
			return info is IMavlinkMessageInfo<T> typed
				? MavlinkV2FrameWriter.Write(in message, typed, seq, systemId, componentId, buffer, signer)
				: MavlinkV2FrameWriter.Write(message, info, seq, systemId, componentId, buffer, signer);
		}
	}
}
