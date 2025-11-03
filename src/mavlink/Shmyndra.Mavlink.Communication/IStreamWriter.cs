namespace Shmyndra.Mavlink.Communication;

public interface IStreamWriter
{
	Task WriteAsync(byte[] buffer, int offset, int length);
}
