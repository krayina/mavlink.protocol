namespace Shmyndra.Mavlink.Generator;

public static class GeneratedMavlinkMessageFieldTypeExtensions
{
	/// <summary>
	/// Determines if the underlying C# type of the MAVLink field is a floating-point number.
	/// </summary>
	/// <param name="fieldType">The field type to check.</param>
	/// <returns><c>true</c> if the converted type is "float" or "double"; otherwise, <c>false</c>.</returns>
	public static bool IsFloatingPoint(this GeneratedMavlinkMessageFieldType fieldType)
	{
		return fieldType.ConvertedType == "float" || fieldType.ConvertedType == "double";
	}

	/// <summary>
	/// Gets the element type if the field is an array; otherwise, returns the type itself.
	/// This is a safe way to "unwrap" an array type to reason about its contents.
	/// </summary>
	public static GeneratedMavlinkMessageFieldType GetElementTypeOrSelf(this GeneratedMavlinkMessageFieldType fieldType)
	{
		if (fieldType is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			return arrayType.ElementType;
		}

		return fieldType;
	}
}
