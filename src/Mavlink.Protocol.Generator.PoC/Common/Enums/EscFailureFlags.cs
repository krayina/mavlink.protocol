namespace Mavlink.Common;

// Underlying type = widest wire type the enum is used with (ESC_INFO.failure_flags: uint16_t[4]).
[global::System.Flags]
public enum EscFailureFlags : ushort
{
	None = 0,
	OverCurrent = 1,
	OverVoltage = 2,
	OverTemperature = 4,
	OverRpm = 8,
	InconsistentCmd = 16,
	MotorStuck = 32,
	Generic = 64,
}
