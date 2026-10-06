namespace Mavlink.Transport;

public interface IMavlinkRawFrameListener
{
	/// <summary>
	/// Called with a complete. Frame (header..crc[..signature]).
	/// </summary>
	void OnFrame(MavlinkFrameDirection direction, ReadOnlySpan<byte> frame);
}
