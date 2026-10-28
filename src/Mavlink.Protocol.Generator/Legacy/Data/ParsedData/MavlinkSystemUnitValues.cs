using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Provides a mapping between MavlinkSystemUnit enum values and their string representations.
/// </summary>
public static class MavlinkSystemUnitValues
{
	private static readonly ImmutableDictionary<MavlinkSystemUnit, string> _values = new Dictionary<MavlinkSystemUnit, string>
	{
		{ MavlinkSystemUnit.S, "s" },
		{ MavlinkSystemUnit.Ds, "ds" },
		{ MavlinkSystemUnit.Cs, "cs" },
		{ MavlinkSystemUnit.Ms, "ms" },
		{ MavlinkSystemUnit.Us, "us" },
		{ MavlinkSystemUnit.Ns, "ns" },
		{ MavlinkSystemUnit.Hz, "Hz" },
		{ MavlinkSystemUnit.MHz, "MHz" },
		{ MavlinkSystemUnit.Km, "km" },
		{ MavlinkSystemUnit.Dam, "dam" },
		{ MavlinkSystemUnit.M, "m" },
		{ MavlinkSystemUnit.MS, "m/s" },
		{ MavlinkSystemUnit.MSS, "m/s/s" },
		{ MavlinkSystemUnit.MS5, "m/s*5" },
		{ MavlinkSystemUnit.Dm, "dm" },
		{ MavlinkSystemUnit.DmS, "dm/s" },
		{ MavlinkSystemUnit.Cm, "cm" },
		{ MavlinkSystemUnit.Cm2, "cm^2" },
		{ MavlinkSystemUnit.CmS, "cm/s" },
		{ MavlinkSystemUnit.Mm, "mm" },
		{ MavlinkSystemUnit.MmS, "mm/s" },
		{ MavlinkSystemUnit.MmH, "mm/h" },
		{ MavlinkSystemUnit.K, "K" },
		{ MavlinkSystemUnit.DegC, "degC" },
		{ MavlinkSystemUnit.CdegC, "cdegC" },
		{ MavlinkSystemUnit.Rad, "rad" },
		{ MavlinkSystemUnit.RadS, "rad/s" },
		{ MavlinkSystemUnit.MradS, "mrad/s" },
		{ MavlinkSystemUnit.Deg, "deg" },
		{ MavlinkSystemUnit.Deg2, "deg/2" },
		{ MavlinkSystemUnit.DegS, "deg/s" },
		{ MavlinkSystemUnit.Cdeg, "cdeg" },
		{ MavlinkSystemUnit.CdegS, "cdeg/s" },
		{ MavlinkSystemUnit.DegE5, "degE5" },
		{ MavlinkSystemUnit.DegE7, "degE7" },
		{ MavlinkSystemUnit.Rpm, "rpm" },
		{ MavlinkSystemUnit.V, "V" },
		{ MavlinkSystemUnit.Cv, "cV" },
		{ MavlinkSystemUnit.Mv, "mV" },
		{ MavlinkSystemUnit.A, "A" },
		{ MavlinkSystemUnit.Ca, "cA" },
		{ MavlinkSystemUnit.Ma, "mA" },
		{ MavlinkSystemUnit.MAh, "mAh" },
		{ MavlinkSystemUnit.Ah, "Ah" },
		{ MavlinkSystemUnit.Mt, "mT" },
		{ MavlinkSystemUnit.Gauss, "gauss" },
		{ MavlinkSystemUnit.Mgauss, "mgauss" },
		{ MavlinkSystemUnit.Hj, "hJ" },
		{ MavlinkSystemUnit.W, "W" },
		{ MavlinkSystemUnit.Mg, "mG" },
		{ MavlinkSystemUnit.G, "g" },
		{ MavlinkSystemUnit.Kg, "kg" },
		{ MavlinkSystemUnit.Pa, "Pa" },
		{ MavlinkSystemUnit.HPa, "hPa" },
		{ MavlinkSystemUnit.KPa, "kPa" },
		{ MavlinkSystemUnit.Mbar, "mbar" },
		{ MavlinkSystemUnit.Empty, "%" },
		{ MavlinkSystemUnit.D, "d%" },
		{ MavlinkSystemUnit.C, "c%" },
		{ MavlinkSystemUnit.Db, "dB" },
		{ MavlinkSystemUnit.DBm, "dBm" },
		{ MavlinkSystemUnit.KiB, "KiB" },
		{ MavlinkSystemUnit.KiBS, "KiB/s" },
		{ MavlinkSystemUnit.MiB, "MiB" },
		{ MavlinkSystemUnit.MiBS, "MiB/s" },
		{ MavlinkSystemUnit.Bytes, "bytes" },
		{ MavlinkSystemUnit.BytesS, "bytes/s" },
		{ MavlinkSystemUnit.BitsS, "bits/s" },
		{ MavlinkSystemUnit.Pix, "pix" },
		{ MavlinkSystemUnit.Dpix, "dpix" },
		{ MavlinkSystemUnit.GMin, "g/min" },
		{ MavlinkSystemUnit.Cm3Min, "cm^3/min" },
		{ MavlinkSystemUnit.Cm3, "cm^3" },
		{ MavlinkSystemUnit.L, "l" }
	}.ToImmutableDictionary();

	/// <summary>
	/// Gets the string representation of the specified MavlinkSystemUnit.
	/// </summary>
	/// <param name="unit">The MavlinkSystemUnit to get the string representation for.</param>
	/// <returns>The string representation of the specified MavlinkSystemUnit.</returns>
	public static string Get(this MavlinkSystemUnit unit) => _values[unit];
}
