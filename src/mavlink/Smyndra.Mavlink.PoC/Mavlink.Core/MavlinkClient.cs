using System.Buffers;

namespace Mavlink;

public sealed class MavlinkClient
{
	private byte _sequence = 0; // Поточний sequence number

	// ... інші поля: systemId, componentId, stream для відправки

	public ValueTask SendAsync<T>(T message, CancellationToken ct = default) where T : IMavlinkMessage
	{
		var info = MavlinkDialectRegistry.GetInfo<T>();
		if (info == null)
		{
			throw new ArgumentException($"Message type {typeof(T).Name} is not supported by the currently loaded dialects.");
		}

		byte[] buffer = ArrayPool<byte>.Shared.Rent(MavlinkPacket.MAX_PACKET_SIZE);
		try
		{
			var packetLength = MavlinkPacketSerializer.Serialize(message, info, _sequence++, /*sysId, compId,*/ buffer);

			return _stream.WriteAsync(buffer.AsMemory(0, packetLength), ct);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public ValueTask SendAsync(IMavlinkMessage message, CancellationToken ct = default)
	{
		if (message == null)
		{
			throw new ArgumentNullException(nameof(message));
		}

		var info = MavlinkDialectRegistry.GetInfo(message.GetType());
		if (info == null)
		{
			throw new ArgumentException($"Message type {message.GetType().Name} is not supported...");
		}

		byte[] buffer = ArrayPool<byte>.Shared.Rent(MavlinkPacket.MAX_PACKET_SIZE);
		try
		{
			int payloadLength = info.SerializePayload(message, buffer.AsSpan(MavlinkPacket.HEADER_LENGTH));

			// ... Заповнюємо header, рахуємо CRC і відправляємо ...
			// Цю логіку краще винести в окремий PacketSerializer
			var packetLength = MavlinkPacketSerializer.AssemblePacket(info.MessageId, info.CrcExtra, payloadLength, _sequence++, buffer);
			return _stream.WriteAsync(buffer.AsMemory(0, packetLength), ct);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
