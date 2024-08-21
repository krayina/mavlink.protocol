using RJCP.IO.Ports;

namespace Shmyndra.Mavlink;

public class SerialStream
{
	private SerialPortStream _baseStream;
	private readonly List<byte> _buffer = new List<byte>();

	public event EventHandler<byte[]>? PacketReceived;

	public SerialStream(string portName, int baudRate)
	{
		_baseStream = new SerialPortStream(portName, baudRate);
	}

	public void StartReading()
	{
		_baseStream.Open();
		Task.Run(() =>
		{
			while (_baseStream.IsOpen)
			{
				byte[] buffer = new byte[256];
				int bytesRead = _baseStream.Read(buffer, 0, buffer.Length);

				if (bytesRead > 0)
				{
					_buffer.AddRange(buffer.Take(bytesRead));
					ProcessBuffer();
				}
			}
		});
	}

	private void ProcessBuffer()
	{
		// Простий приклад обробки буфера для виявлення пакетів Mavlink.
		while (_buffer.Count > 0)
		{
			// Виявлення початку пакету (наприклад, за заголовком)
			if (_buffer[0] == 0xFE || _buffer[0] == 0xFD) // Mavlink V1 або V2
			{
				// Визначення довжини пакету (залежно від версії протоколу)
				int packetLength = GetMavlinkPacketLength(_buffer);

				if (_buffer.Count >= packetLength)
				{
					byte[] packet = _buffer.Take(packetLength).ToArray();
					_buffer.RemoveRange(0, packetLength);

					// Виклик події для передачі пакету
					PacketReceived?.Invoke(this, packet);
				}
				else
				{
					break; // Очікуємо більше даних
				}
			}
			else
			{
				_buffer.RemoveAt(0); // Видалити зайві байти
			}
		}
	}

	private int GetMavlinkPacketLength(List<byte> buffer)
	{
		// Реалізація визначення довжини пакету (залежно від версії)
		if (buffer[0] == 0xFD)
		{
			return 10 + buffer[1] + 2; // Mavlink V2 (з заголовком і CRC)
		}
		else
		{
			return 6 + buffer[1] + 2; // Mavlink V1 (з заголовком і CRC)
		}
	}
}
