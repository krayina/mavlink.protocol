using Console = System.Diagnostics.Debug;

namespace Shmyndra.Mavlink.Protocol.v3_0;

public class DroneCommunication
{
	private readonly TcpStream _tcpStream;
	private readonly byte[] _buffer = new byte[1024];

	public event EventHandler<byte[]>? PacketReceived;

	public DroneCommunication(string url)
	{
		_tcpStream = new TcpStream(url);
	}

	public void Connect()
	{
		try
		{
			_tcpStream.Open();
			Console.WriteLine("Connected to the drone.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error connecting to the drone: {ex.Message}");
		}
	}

	public void ListenForMessages()
	{
		while (_tcpStream.IsOpen)
		{
			try
			{
				int bytesRead = _tcpStream.Read(_buffer, 0, _buffer.Length);
				if (bytesRead > 0)
				{
					PacketReceived?.Invoke(this, _buffer);
					//ProcessReceivedData(_buffer, bytesRead);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading from the stream: {ex.Message}");
			}
		}
	}

	//private void ProcessReceivedData(byte[] data, int length)
	//{
	//	// Assuming data is a MAVLink message, you would parse it here
	//	var heartbeat = new MavlinkTypes.Minimal.Heartbeat
	//	{
	//		Type = (MavlinkTypes.Minimal.MavType)data[0],
	//		Autopilot = (MavlinkTypes.Minimal.MavAutopilot)data[1],
	//		BaseMode = (MavlinkTypes.Minimal.MavModeFlag)data[2],
	//		CustomMode = BitConverter.ToUInt32(data, 3),
	//		SystemStatus = (MavlinkTypes.Minimal.MavState)data[7],
	//		MavlinkVersion = data[8]
	//	};

	//	Console.WriteLine("Received Heartbeat:");
	//	Console.WriteLine($"Type: {heartbeat.Type}");
	//	Console.WriteLine($"Autopilot: {heartbeat.Autopilot}");
	//	Console.WriteLine($"BaseMode: {heartbeat.BaseMode}");
	//	Console.WriteLine($"CustomMode: {heartbeat.CustomMode}");
	//	Console.WriteLine($"SystemStatus: {heartbeat.SystemStatus}");
	//	Console.WriteLine($"MavlinkVersion: {heartbeat.MavlinkVersion}");
	//}	

	//private void ProcessReceivedData(byte[] data, int length)
	//{
	//	var test = MavlinkV1PacketConverter.FromByteArray(data);
	//	// Assuming data is a MAVLink message, you would parse it here
	//	var heartbeat = new MavlinkTypes.Minimal.Heartbeat
	//	{
	//		Type = (MavlinkTypes.Minimal.MavType)data[0],
	//		Autopilot = (MavlinkTypes.Minimal.MavAutopilot)data[1],
	//		BaseMode = (MavlinkTypes.Minimal.MavModeFlag)data[2],
	//		CustomMode = BitConverter.ToUInt32(data, 3),
	//		SystemStatus = (MavlinkTypes.Minimal.MavState)data[7],
	//		MavlinkVersion = data[8]
	//	};

	//	Console.WriteLine("Received Heartbeat:");
	//	Console.WriteLine($"Type: {heartbeat.Type}");
	//	Console.WriteLine($"Autopilot: {heartbeat.Autopilot}");
	//	Console.WriteLine($"BaseMode: {heartbeat.BaseMode}");
	//	Console.WriteLine($"CustomMode: {heartbeat.CustomMode}");
	//	Console.WriteLine($"SystemStatus: {heartbeat.SystemStatus}");
	//	Console.WriteLine($"MavlinkVersion: {heartbeat.MavlinkVersion}");
	//}

	public void Disconnect()
	{
		try
		{
			_tcpStream.Close();
			Console.WriteLine("Disconnected from the drone.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error disconnecting from the drone: {ex.Message}");
		}
	}
}
