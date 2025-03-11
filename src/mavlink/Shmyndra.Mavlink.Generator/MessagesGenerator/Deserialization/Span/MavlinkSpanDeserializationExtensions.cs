namespace Shmyndra.Mavlink.Generator;

internal static class MavlinkSpanDeserializationExtensions
{
	public static string GetBinaryPrimitivesMethod(string typeName) => typeName switch
	{
		"int" => "ReadInt32LittleEndian",
		"uint" => "ReadUInt32LittleEndian",
		"short" => "ReadInt16LittleEndian",
		"ushort" => "ReadUInt16LittleEndian",
		"char" => "ReadUInt16LittleEndian",
		"long" => "ReadInt64LittleEndian",
		"ulong" => "ReadUInt64LittleEndian",
		"float" => "ReadSingleLittleEndian",
		"double" => "ReadDoubleLittleEndian",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
