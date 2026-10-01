namespace Mavlink.Bitmask;

/// <summary>
/// Display metadata for one defined flag bit.
/// </summary>
public readonly struct BitmaskEntry : IEquatable<BitmaskEntry>
{
	public BitmaskEntry(int bit, string name, string wireName)
	{
		Bit = bit;
		Name = name;
		WireName = wireName;
	}

	/// <summary>
	/// Bit index, 0..63.
	/// </summary>
	public int Bit { get; }

	/// <summary>
	/// C# enum member name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// MAVLink XML entry name.
	/// </summary>
	public string WireName { get; }

	public bool Equals(BitmaskEntry other) =>
		Bit == other.Bit && Name == other.Name && WireName == other.WireName;
	public override bool Equals(object? obj) => obj is BitmaskEntry other && Equals(other);
	public override int GetHashCode() => Bit;
	public static bool operator ==(BitmaskEntry l, BitmaskEntry r) => l.Equals(r);
	public static bool operator !=(BitmaskEntry l, BitmaskEntry r) => !l.Equals(r);
}
