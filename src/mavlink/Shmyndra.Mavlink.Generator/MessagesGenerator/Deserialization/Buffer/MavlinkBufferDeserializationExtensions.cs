namespace Shmyndra.Mavlink.Generator;

internal class MavlinkBufferDeserializationExtensions
{
	public static string GetBitConverterMethod(string typeName) => typeName switch
	{
		"int" => "ToInt32",
		"uint" => "ToUInt32",
		"short" => "ToInt16",
		"ushort" => "ToUInt16",
		"long" => "ToInt64",
		"ulong" => "ToUInt64",
		"float" => "ToSingle",
		"double" => "ToDouble",
		"byte" => "ToByte",
		"sbyte" => "ToSByte",
		"char" => "ToChar",
		_ => throw new NotSupportedException($"Unsupported type: {typeName}")
	};
}
