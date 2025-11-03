namespace Shmyndra.Mavlink.Communication;

public interface IStreamStrategy
{
	IStreamConnection GetConnection();
}
