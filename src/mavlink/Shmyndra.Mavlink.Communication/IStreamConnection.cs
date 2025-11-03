namespace Shmyndra.Mavlink.Communication;

public interface IStreamConnection : IDisposable
{
	bool IsOpen { get; }
	Task ConnectAsync();
	void Disconnect();
}
