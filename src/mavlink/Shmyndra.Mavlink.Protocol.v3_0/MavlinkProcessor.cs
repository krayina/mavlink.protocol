using System.Diagnostics;
using Shmyndra.Mavlink.V1;
using Shmyndra.Mavlink.V2;
using MavlinkTypes;

namespace Shmyndra.Mavlink.Protocol.v3_0;

public class MavlinkProcessor
{
	public void ProcessData(byte[] data)
	{
		bool isMavlinkV2 = data[0] == 0xFD;

		if (isMavlinkV2)
		{
			var packetV2 = MavlinkV2PacketConverter.FromByteArray(data);
			ProcessPacket(packetV2.MessageIdentifier, packetV2.Payload);
		}
		else
		{
			var packetV1 = MavlinkV1PacketConverter.FromByteArray(data);
			ProcessPacket(packetV1.MessageIdentifier, packetV1.Payload);
		}
	}

	private void ProcessPacket(uint messageId, byte[] payload)
	{
		try
		{
			if (MavlinkMessages.TryCreateMessageInstance(messageId, payload, out var messageInstance))
			{
				Debug.WriteLine($"Processed message ID: {messageId}, Type: {messageInstance.GetType().Name}");
			}
			else
			{
				Debug.WriteLine($"Unknown or unsupported message ID: {messageId}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"____Exception: {ex}");
		}
	}
}
