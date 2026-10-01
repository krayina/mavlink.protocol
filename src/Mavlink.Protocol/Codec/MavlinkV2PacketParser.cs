using Mavlink.Dialects;

namespace Mavlink;

internal static class MavlinkV2PacketParser
{
	public static MavlinkPacketParseResult TryParse(
		ReadOnlySpan<byte> raw,
		IMavlinkDialect dialect,
		out MavlinkReceivedPacket packet)
	{
		packet = default;

		if (raw.Length < MavlinkConstants.HEADER_V2_LENGTH + 2)
		{
			return MavlinkPacketParseResult.InvalidFrameLength;
		}

		var frame = new MavlinkV2Frame(raw);

		var info = dialect.GetInfo(frame.MessageId);
		if (info == null)
		{
			return MavlinkPacketParseResult.UnknownMessageId;
		}

		ushort computed = X25Crc.Calculate(frame.CrcRegion);
		computed = X25Crc.Accumulate(computed, info.CrcExtra);
		if (frame.ReceivedCrc != computed)
		{
			return MavlinkPacketParseResult.CrcMismatch;
		}

		packet = new MavlinkReceivedPacket(
			frame.MessageId,
			frame.SystemId,
			frame.ComponentId,
			frame.Sequence,
			MavlinkPacketVersion.V2,
			frame.IsSigned,
			frame.Payload);

		return MavlinkPacketParseResult.Success;
	}
}
