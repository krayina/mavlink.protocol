//namespace Shmyndra.Mavlink;

//internal class MavCommunicatorBase
//{
//	private void AA()
//	{
//		using TcpStream tcpStream = new TcpStream("127.0.0.1:5762");
//		tcpStream.ReadBufferSize = 16 * 1024;
//		tcpStream.Open();
//		tcpStream.DiscardInBuffer();

//		var startConnect = DateTime.Now;
//		var timeout = startConnect.Add(TimeSpan.FromSeconds(30));
//		var alarmTimeout = startConnect.Add(TimeSpan.FromSeconds(20));

//		while (DateTime.Now < timeout)
//		{
//			var messageList = hbHistory.GroupBy(s => MavComponentList.GetId(s.sysid, s.compid))
//						.OrderByDescending(s => s.Count()).Where(s => s.Count() >= 4);
//			foreach (var bestMessage in messageList)
//			{
//				var bestHbCount = bestMessage.Count();
//				var msg = bestMessage.Last();

//				// preference compId of 1, failOver to anything that we have seen 4 times
//				if (bestHbCount >= HeartbeatCountToAccept && msg.compid == MainComponentId
//					/* || bestHbCount >= HeartbeatCountToAccept * 2*/)
//				{
//					SetupConnect(msg);
//					success = true;
//					break;
//				}
//			}

//		}
//	}

//	private void SendGscHeartbeat(bool ignoreMavList = false)
//	{
//		var heartbeat = new MavlinkTypes.Minimal.Heartbeat
//		{
//			Type = MavlinkTypes.Minimal.MavType.MavTypeGcs,
//			Autopilot = MavlinkTypes.Minimal.MavAutopilot.MavAutopilotInvalid,
//			MavlinkVersion = MavlinkTypes.Minimal.MavlinkSpecification.Version!.Value
//		};

//		if (ignoreMavList)
//		{
//			SendPacket(MavlinkTypes.MavlinkMessages.GetId<MavlinkTypes.Minimal.Heartbeat>(), heartbeat, 0, 0);
//			return;
//		}

//		// Logger.Debug(@"[SEND GSC Heartbeat]");
//		//foreach (var mav in MavList)
//		//	SendPacket(MAVLINK_MSG_ID.HEARTBEAT, htb, mav.SysId, mav.CompId);
//	}

//	protected void SendPacket(ulong messageId, object rawData, int sysId = -1, int compId = -1, bool forceMavlink2 = false)
//	{
//		try
//		{
//			sysId = sysId < 0 ? SysIdCurrent : sysId;
//			//compId = compId < 0 ? CompIdCurrent : compId;
//			//var packet = MavList[sysId, compId].IsMavLink2 || forceMavlink2
//			//	? CommunicatorUtils.GenerateMav2Packet(msgId, rawData, MavList[sysId, compId], ref _packetCounter)
//			//	: CommunicatorUtils.GenerateMav1Packet(msgId, rawData, ref _packetCounter);

//			//lock (_locker)
//			//{
//			//	if (_baseStream is not { IsOpen: true } || _isTelemetryPlayback)
//			//		return;

//			//	_baseStream.Write(packet, 0, packet.Length);
//			//}
//			//MavList[sysId, compId].State.PacketSendCount++;
//			//SaveToTLog(packet, 0, packet.Length);
//			//SendMavlinkStat();
//		}
//		catch (Exception e)
//		{
//			System.Diagnostics.Debug.WriteLine($"Exception at send packet: {e.Message}\n{e.StackTrace}");
//		}
//	}
//}
