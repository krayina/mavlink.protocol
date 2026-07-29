using System.Text;

namespace Shmyndra.Mavlink;

/// <summary>
/// Provides methods for calculating X.25 CRC checksums, commonly used in MAVLink protocol.
/// </summary>
public static class X25Crc
{
	/// <summary>
	/// The initial seed value for the CRC calculation.
	/// </summary>
	public const ushort CrcSeed = 0xFFFF;

	/// <summary>
	/// Calculates the CRC for a given byte array with optional extra bytes.
	/// </summary>
	/// <param name="data">The byte array to calculate the CRC for.</param>
	/// <param name="start">The starting index in the array.</param>
	/// <param name="length">The number of bytes to include in the calculation.</param>
	/// <param name="extraBytes">Optional extra bytes to include at the end of the CRC calculation.</param>
	/// <returns>The calculated CRC as a ushort.</returns>
	public static ushort Calculate(byte[] data, int start, int length, params byte[] extraBytes)
	{
		ushort crc = CrcSeed;

		// Accumulate CRC for the given data
		for (int i = start; i < length; i++)
		{
			crc = Accumulate(crc, data[i]);
		}

		// If extra bytes are provided, include them in the CRC calculation
		foreach (var extra in extraBytes)
		{
			crc = Accumulate(crc, extra);
		}

		return crc;
	}

	/// <summary>
	/// Accumulates the CRC checksum for a given string using the X.25 algorithm.
	/// </summary>
	/// <param name="s">The input string to accumulate the CRC for.</param>
	/// <param name="crc">The initial CRC value to start with.</param>
	/// <returns>The updated CRC value after processing the input string.</returns>
	public static ushort Accumulate(string s, ushort crc)
	{
		var bytes = Encoding.GetEncoding(28591).GetBytes(s);
		return bytes.Aggregate(crc, Accumulate);
	}

	/// <summary>
	/// Accumulates the CRC checksum for a given byte using the X.25 algorithm.
	/// </summary>
	/// <param name="crc">The initial CRC value to start with.</param>
	/// <param name="b">The byte to accumulate the CRC for.</param>
	/// <returns>The updated CRC value after processing the input byte.</returns>
	public static ushort Accumulate(ushort crc, byte b)
	{
		unchecked
		{
			var ch = (byte)(b ^ (byte)(crc & 0x00FF));
			ch = (byte)(ch ^ (ch << 4));
			return (ushort)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
		}
	}

	/// <summary>
	/// Finalizes the CRC calculation and returns the final checksum value.
	/// </summary>
	/// <param name="crc">The accumulated CRC value.</param>
	/// <returns>The final CRC checksum as a byte.</returns>
	public static byte FinalizeCrc(ushort crc)
	{
		return (byte)((crc & 0xFF) ^ (crc >> 8));
	}
}
