using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Shmyndra.Mavlink;

public class TcpStream : IDisposable
{
	private static readonly char Separtator = ':';

	public bool AutoReconnect;
	public TcpClient Client = new();
	private bool _inOpen;
	private DateTime _lastReconnectTime = DateTime.MinValue;

	public int Retrys = 3;

	public TcpStream(string url)
	{
		var data = url.Split(Separtator);
		if (data.Length != 2)
			throw new Exception("Invalid url format need: 127.0.0.1:5656");

		Port = data.LastOrDefault();
		Host = data.FirstOrDefault();
	}

	public string Port { get; set; }
	public string Host { get; set; }

	public int WriteBufferSize { get; set; }
	public int WriteTimeout { get; set; }
	public bool RtsEnable { get; set; }
	public Stream BaseStream => Client.GetStream();

	public int ReadTimeout
	{
		get; // { return client.ReceiveTimeout; }
		set; // { client.ReceiveTimeout = value; }
	} = 500;

	public int ReadBufferSize { get; set; }

	public int BaudRate { get; set; }

	public int DataBits { get; set; }

	public string PortName
	{
		get => Port;
		set { }
	}

	public string ConnectionString => $"{Host}{Separtator}{Port}";

	public int BytesToRead => Client.Available;

	public int BytesToWrite => 0;

	public bool IsOpen
	{
		get
		{
			try
			{
				if (Client == null) return false;
				if (Client.Client == null) return false;

				if (AutoReconnect && Client.Client.Connected == false && !_inOpen)
					DoAutoReconnect();

				return Client.Client.Connected;
			}
			catch
			{
				return false;
			}
		}
	}

	public bool DtrEnable { get; set; }

	public void Open()
	{
		try
		{
			_inOpen = true;

			if (Client.Client.Connected)
			{
				//Log.Warn("tcpserial socket already open");
				return;
			}


			Client = new TcpClient()
			{
				NoDelay = true,
				Client =
					{
						NoDelay = true
					}
			};
			Client.Connect(Host, int.Parse(Port, CultureInfo.InvariantCulture));

			VerifyConnected();
		}
		catch
		{
			// disable if the first connect fails
			AutoReconnect = false;
			throw;
		}
		finally
		{
			_inOpen = false;
		}
	}

	public int Read(byte[] readto, int offset, int length)
	{
		VerifyConnected();
		try
		{
			if (length < 1) return 0;

			return Client!.Client.Receive(readto, offset, length, SocketFlags.None);
			/*
							byte[] temp = new byte[length];
							clientbuf.Read(temp, 0, length);

							temp.CopyTo(readto, offset);

							return length;*/
		}
		catch
		{
			throw new Exception("Socket Closed");
		}
	}

	public int ReadByte()
	{
		VerifyConnected();
		var count = 0;
		while (BytesToRead == 0)
		{
			Thread.Sleep(1);
			if (count > ReadTimeout)
				throw new Exception("NetSerial Timeout on read");
			count++;
		}

		var buffer = new byte[1];
		Read(buffer, 0, 1);
		return buffer[0];
	}

	public int ReadChar()
	{
		return ReadByte();
	}

	public string ReadExisting()
	{
		VerifyConnected();

		if (Client is not null)
		{
			var data = new byte[Client.Available];
			if (data.Length > 0)
				Read(data, 0, data.Length);

			var line = Encoding.ASCII.GetString(data, 0, data.Length);
			return line;
		}
		return string.Empty;
	}

	public void WriteLine(string line)
	{
		VerifyConnected();
		line = line + "\n";
		Write(line);
	}

	public Task ToggleDtrRts()
	{
		return Task.CompletedTask;
	}

	public void Write(string line)
	{
		VerifyConnected();
		var data = new ASCIIEncoding().GetBytes(line);
		Write(data, 0, data.Length);
	}

	public void Write(byte[] write, int offset, int length)
	{
		VerifyConnected();
		try
		{
			Client?.Client.Send(write, length, SocketFlags.None);
		}
		catch
		{
		} //throw new Exception("Comport / Socket Closed"); }
	}

	public void DiscardInBuffer()
	{
		VerifyConnected();
		if (Client is not null)
		{
			var size = Client.Available;
			var crap = new byte[size];
			//Log.InfoFormat("TcpSerial DiscardInBuffer {0}", size);
			Read(crap, 0, size);
		}
	}

	public string ReadLine()
	{
		var temp = new byte[4000];
		var count = 0;
		var timeout = 0;

		while (timeout <= 100)
		{
			if (!IsOpen) break;
			if (BytesToRead > 0)
			{
				var letter = (byte)ReadByte();

				temp[count] = letter;

				if (letter == '\n') // normal line
					break;

				count++;
				if (count == temp.Length)
					break;
				timeout = 0;
			}
			else
			{
				timeout++;
				Thread.Sleep(5);
			}
		}

		Array.Resize(ref temp, count + 1);

		return Encoding.ASCII.GetString(temp, 0, temp.Length);
	}

	public void Close()
	{
		try
		{
			if (Client?.Client != null && Client.Client.Connected)
			{
				Client.Client.Dispose();
				Client.Dispose();
			}
		}
		catch
		{
		}

		try
		{
			Client?.Dispose();
		}
		catch
		{
		}

		Client = new TcpClient();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void DoAutoReconnect()
	{
		if (!AutoReconnect)
			return;
		try
		{
			if (DateTime.Now > _lastReconnectTime)
			{
				try
				{
					Client?.Dispose();
				}
				catch
				{
				}

				Client = new TcpClient();

				//Log.InfoFormat("doAutoReconnect {0} {1}", Host, Port);

				var task = Client.ConnectAsync(Host, int.Parse(Port, CultureInfo.InvariantCulture));

				_lastReconnectTime = DateTime.Now.AddSeconds(5);
			}
		}
		catch
		{
		}
	}

	private void VerifyConnected()
	{
		if (!IsOpen)
		{
			try
			{
				Client?.Dispose();
			}
			catch
			{
			}

			// this should only happen if we have established a connection in the first place
			if (Client != null && Retrys > 0)
			{
				//Log.Info("tcp reconnect");
				Client = new TcpClient();
				Client.Connect(Host, int.Parse(Port, CultureInfo.InvariantCulture));
				Retrys--;
			}

			throw new Exception("The socket/serialproxy is closed");
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			// dispose managed resources
			Close();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			Client = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		}

		// free native resources
	}
}
