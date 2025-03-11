namespace Shmyndra.Mavlink.Generator;

internal class MavlinkSpanSerializationExtensions
{
	public static string GetBinaryPrimitivesWriteMethod(string typeName) => typeName switch
	{
		"short" => "WriteInt16LittleEndian",
		"ushort" => "WriteUInt16LittleEndian",
		"int" => "WriteInt32LittleEndian",
		"uint" => "WriteUInt32LittleEndian",
		"long" => "WriteInt64LittleEndian",
		"ulong" => "WriteUInt64LittleEndian",
		_ => throw new NotSupportedException($"Type '{typeName}' is not supported for BinaryPrimitives serialization.")
	};
}
