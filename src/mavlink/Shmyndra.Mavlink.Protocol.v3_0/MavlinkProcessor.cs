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
			var properties = messageType.GetProperties();
			int offset = 0;

			Debug.WriteLine($"ID:{messageId}");
			foreach (var property in properties)
			{
				var value = ReadValueFromPayload(property.PropertyType, payload, ref offset);
				Debug.WriteLine($"\t{property.Name}: {value}");
			}
		}
		else
		{
			Debug.WriteLine($"Unknown message ID: {messageId}");
		}
	}

	private dynamic? ReadValueFromPayload(Type type, byte[] payload, ref int offset)
	{
		dynamic? value = null;

		if (type == typeof(byte))
		{
			value = payload[offset];
			offset += sizeof(byte);
		}
		else if (type == typeof(uint))
		{
			value = BitConverter.ToUInt32(payload, offset);
			offset += sizeof(uint);
		}
		else if (type == typeof(int))
		{
			value = BitConverter.ToInt32(payload, offset);
			offset += sizeof(int);
		}
		else if (type == typeof(short))
		{
			value = BitConverter.ToInt16(payload, offset);
			offset += sizeof(short);
		}
		else if (type == typeof(ushort))
		{
			value = BitConverter.ToUInt16(payload, offset);
			offset += sizeof(ushort);
		}
		else if (type == typeof(float))
		{
			value = BitConverter.ToSingle(payload, offset);
			offset += sizeof(float);
		}
		// Додайте підтримку інших типів даних за потреби

		return value;
	}
}
