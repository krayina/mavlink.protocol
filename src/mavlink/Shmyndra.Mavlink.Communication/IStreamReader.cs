namespace Shmyndra.Mavlink.Communication;

public interface IStreamReader
{
	event EventHandler<byte[]>? PacketReceived;
}
