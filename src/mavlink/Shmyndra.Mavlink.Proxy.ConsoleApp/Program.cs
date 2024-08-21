using Shmyndra.Mavlink.Protocol.v3_0;

namespace Shmyndra.Mavlink.Proxy.ConsoleApp;

internal class Program
{
	static async Task Main(string[] args)
	{
		var processor = new MavlinkProcessor();


		var droneComm = new DroneCommunication("127.0.0.1:5760");

		//SerialStream serialStream = new SerialStream("COM6", 57600);
		droneComm.PacketReceived += (sender, data) =>
		//serialStream.PacketReceived += (sender, data) =>
		{
			processor.ProcessData(data);
			//Console.WriteLine("Press any key to disconnect...");
		};
		//serialStream.StartReading();

		droneComm.Connect();
		Thread listenerThread = new Thread(droneComm.ListenForMessages);
		listenerThread.Start();

		Console.WriteLine("Press any key to disconnect...");
		Console.ReadKey();


		droneComm.Disconnect();

		//Console.WriteLine("Hello, World!");
		//await Task.Delay(20_000);
	}
}
