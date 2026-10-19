namespace Mavlink.Common;

public enum EscConnectionType : byte
{
	Ppm = 0,
	Serial = 1,
	Oneshot = 2,
	I2c = 3,
	Can = 4,
	Dshot = 5,
}
