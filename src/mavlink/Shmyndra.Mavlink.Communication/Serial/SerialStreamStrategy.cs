namespace Shmyndra.Mavlink.Communication.Serial;

public class SerialStreamStrategy : IStreamStrategy
{
	private readonly string _portName;
	private readonly int _baudRate;

	public SerialStreamStrategy(string portName, int baudRate)
	{
		_portName = portName;
		_baudRate = baudRate;
	}

	public IStreamConnection GetConnection() => new MavlinkSerialStream(_portName, _baudRate);
}
