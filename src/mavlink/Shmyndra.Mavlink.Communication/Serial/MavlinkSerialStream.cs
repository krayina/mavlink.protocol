using RJCP.IO.Ports;

namespace Shmyndra.Mavlink.Communication.Serial;

internal class MavlinkSerialStream : IStreamConnection, IStreamReader, IStreamWriter
{
	protected readonly List<byte> Buffer = new List<byte>();
	private SerialPortStream _baseStream;
	private readonly object _bufferLock = new object();
	private CancellationTokenSource? _cancellationTokenSource;

	public event EventHandler<byte[]>? PacketReceived;

	public MavlinkSerialStream(string portName, int baudRate)
	{
		_baseStream = new SerialPortStream(portName, baudRate);
	}

	public bool IsOpen => _baseStream.IsOpen;

	public async Task ConnectAsync()
	{
		if (IsOpen)
		{
			throw new InvalidOperationException("Connection is already open.");
		}

		_baseStream.Open();
		_cancellationTokenSource = new CancellationTokenSource();

		await Task.Run(async () =>
		{
			while (_baseStream.IsOpen && !_cancellationTokenSource.Token.IsCancellationRequested)
			{
				await ReadAsync(_cancellationTokenSource.Token);
			}
		}, _cancellationTokenSource.Token);
	}

	private async Task ReadAsync(CancellationToken cancellationToken)
	{
		var buffer = new byte[256];
		var bytesRead = await _baseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

		if (bytesRead > 0)
		{
			lock (_bufferLock)
			{
				Buffer.AddRange(buffer.Take(bytesRead));
				ProcessMavlinkBuffer();
			}
		}
	}

	public async Task WriteAsync(byte[] buffer, int offset, int length)
	{
		await _baseStream.WriteAsync(buffer, offset, length);
	}

	public void Disconnect()
	{
		_cancellationTokenSource?.Cancel();
		_baseStream.Close();
	}

	public void Dispose()
	{
		_cancellationTokenSource?.Cancel();
		_baseStream.Dispose();
	}

	protected void ProcessMavlinkBuffer()
	{
		while (Buffer.Count > 0)
		{
			// Mavlink V1 or V2
			if (Buffer[0] == 0xFE || Buffer[0] == 0xFD)
			{
				// Determine the length of the packet (depending on the protocol version)
				var packetLength = GetMavlinkPacketLength(Buffer);

				if (Buffer.Count >= packetLength)
				{
					var packet = Buffer.Take(packetLength).ToArray();
					Buffer.RemoveRange(0, packetLength);

					// Trigger the event to pass the packet
					PacketReceived?.Invoke(this, packet);
				}
				else
				{
					// Wait for more data
					break;
				}
			}
			else
			{
				// Remove extraneous bytes
				Buffer.RemoveAt(0);
			}
		}
	}

	protected int GetMavlinkPacketLength(List<byte> buffer)
	{
		// Implementation for determining the packet length (depending on the version)
		if (buffer[0] == 0xFD)
		{
			// Mavlink V2 (with header and CRC)
			return 10 + buffer[1] + 2;
		}
		else
		{
			// Mavlink V1 (with header and CRC)
			return 6 + buffer[1] + 2;
		}
	}
}
