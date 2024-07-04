using Shmyndra.Mavlink.Protocol.v3_0;

namespace Shmyndra.Mavlink.Proxy.ConsoleApp;

internal class Program
{
	static async Task Main(string[] args)
	{
		var droneComm = new DroneCommunication("127.0.0.1:5760");
		droneComm.Connect();
		Thread listenerThread = new Thread(droneComm.ListenForMessages);
		listenerThread.Start();

		Console.WriteLine("Press any key to disconnect...");
		Console.ReadKey();

		droneComm.Disconnect();
		//using TcpStream tcpStream = new TcpStream("127.0.0.1:5760");
		//tcpStream.ReadBufferSize = 16 * 1024;
		//tcpStream.Open();
		//tcpStream.DiscardInBuffer();

		//var startConnect = DateTime.Now;
		//var timeout = startConnect.Add(TimeSpan.FromSeconds(30));
		//var alarmTimeout = startConnect.Add(TimeSpan.FromSeconds(20));

		//while (DateTime.Now < timeout)
		//{
		//	var messageList = hbHistory.GroupBy(s => MavComponentList.GetId(s.sysid, s.compid))
		//				.OrderByDescending(s => s.Count()).Where(s => s.Count() >= 4);
		//	foreach (var bestMessage in messageList)
		//	{
		//		var bestHbCount = bestMessage.Count();
		//		var msg = bestMessage.Last();

		//		// preference compId of 1, failOver to anything that we have seen 4 times
		//		if (bestHbCount >= HeartbeatCountToAccept && msg.compid == MainComponentId
		//			/* || bestHbCount >= HeartbeatCountToAccept * 2*/)
		//		{
		//			SetupConnect(msg);
		//			success = true;
		//			break;
		//		}
		//	}

		//}


		//Console.WriteLine("Hello, World!");
		//await Task.Delay(20_000);
	}
}
