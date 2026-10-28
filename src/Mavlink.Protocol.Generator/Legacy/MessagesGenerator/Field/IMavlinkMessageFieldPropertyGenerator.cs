namespace Mavlink.Protocol.Generator;

/// <summary>
/// Defines a contract for generating a MAVLink message property/field representation.
/// </summary>
public interface IMavlinkMessageFieldPropertyGenerator
{
	/// <summary>
	/// Generates a property for a primitive MAVLink field type (e.g., uint8_t, float).
	/// </summary>
	/// <param name="field">The original field definition from the MAVLink XML.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkMessageField"/> representing the generated property.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="field"/> is null.</exception>
	GeneratedMavlinkMessageField GeneratePrimitiveProperty(MavlinkMessageField field);

	/// <summary>
	/// Generates a property for an enum MAVLink field type.
	/// </summary>
	/// <param name="field">The original field definition from the MAVLink XML.</param>
	/// <param name="generatedEnum">The corresponding generated enum information.</param>
	/// <param name="fieldOwnerTypeNamespace">The namespace of the message that will contain this property. Required to resolve the full name of the enum type.</param>
	/// <returns>A new instance of <see cref="GeneratedMavlinkMessageField"/> representing the generated property.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="field"/>, <paramref name="fieldOwnerTypeNamespace"/>, or <paramref name="generatedEnum"/> is null.
	/// This prevents generating an incorrect property if the required enum information is missing.
	/// </exception>
	GeneratedMavlinkMessageField GenerateEnumProperty(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string fieldOwnerTypeNamespace);
}
