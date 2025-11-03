namespace Shmyndra.Mavlink.Communication.Tcp;

public class TcpStreamStrategy : IStreamStrategy
{
	private readonly string _hostName;
	private readonly int _port;

	public TcpStreamStrategy(string hostName, int port)
	{
		_hostName = hostName;
		_port = port;
	}

	public IStreamConnection GetConnection() => new MavlinkTcpStream(_hostName, _port);
}
