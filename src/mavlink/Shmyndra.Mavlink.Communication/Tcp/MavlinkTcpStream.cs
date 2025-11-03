using System.Net.Sockets;

namespace Shmyndra.Mavlink.Communication.Tcp;

internal class MavlinkTcpStream : IStreamConnection, IStreamReader, IStreamWriter
{
	private readonly string _hostName;
	private readonly int _port;
	private readonly TcpClient _client;
	private CancellationTokenSource? _cancellationTokenSource;
	private readonly SocketAwaitable _socketAwaitable;

	public event EventHandler<byte[]>? PacketReceived;


	public MavlinkTcpStream(string hostName, int port)
	{
		_hostName = hostName;
		_port = port;
		_client = new TcpClient();

		var args = new SocketAsyncEventArgs();
		args.SetBuffer(new byte[1024], 0, 1024);
		_socketAwaitable = new SocketAwaitable(args);
	}

	public bool IsOpen => _client.Connected;

	public async Task ConnectAsync()
	{
		if (IsOpen)
		{
			throw new InvalidOperationException("Connection is already open.");
		}

		await _client.ConnectAsync(_hostName, _port);
		_cancellationTokenSource = new CancellationTokenSource();

		await Task.Run(async () =>
		{
			while (IsOpen && !_cancellationTokenSource.Token.IsCancellationRequested)
			{
				await ReadAsync(_cancellationTokenSource.Token);
			}
		}, _cancellationTokenSource.Token);
	}

	private async Task ReadAsync(CancellationToken cancellationToken)
	{
		var socket = _client.Client;
		if (socket == null)
		{
			return;
		}

		_socketAwaitable.Reset();
		await socket.ReceiveAsync(_socketAwaitable);

		var bytesRead = _socketAwaitable.EventArgs.BytesTransferred;

		if (bytesRead > 0)
		{
			var packet = _socketAwaitable.EventArgs.Buffer.Take(bytesRead).ToArray();
			PacketReceived?.Invoke(this, packet);
		}
	}

	public async Task WriteAsync(byte[] buffer, int offset, int length)
	{
		if (buffer == null) throw new ArgumentNullException(nameof(buffer));
		await _client.Client.SendAsync(new ArraySegment<byte>(buffer, offset, length), SocketFlags.None);
	}

	public void Disconnect()
	{
		_cancellationTokenSource?.Cancel();
		_client.Close();
	}

	public void Dispose()
	{
		_cancellationTokenSource?.Cancel();
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_client.Close();
		}
	}
}
