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
		if (MavlinkMessages.TryGetType(messageId, out var messageType))
		{
			var instance = Activator.CreateInstance(messageType);

			// Тут необхідно імплементувати логіку для десеріалізації payload у екземпляр instance

			foreach (var property in messageType.GetProperties())
			{
				var value = property.GetValue(instance);
				if (value != null)
				{
					Debug.WriteLine($"{property.Name}: {value}");
				}
			}
		}
		else
		{
			Debug.WriteLine($"Unknown message ID: {messageId}");
		}
	}
}
