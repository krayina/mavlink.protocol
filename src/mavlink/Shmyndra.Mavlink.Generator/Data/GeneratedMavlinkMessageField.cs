namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents a Mavlink message field that has been generated with an additional generated name.
/// </summary>
/// <remarks>
/// Instances of this class are created exclusively by implementations of the <see cref="IMavlinkMessageTypesGenerator"/> interface
/// and should not be instantiated manually.<br/>
/// The <see cref="MavlinkMessageField.Type"/> property will be an instance of <see cref="GeneratedMavlinkMessageFieldType"/>, 
/// which includes specific types like <see cref="GeneratedMavlinkMessageFieldEnumType"/>, <see cref="GeneratedMavlinkMessageFieldArrayType"/> and <see cref="GeneratedMavlinkMessageFieldArrayEnumType"/> 
/// to represent various Mavlink message field types and their dotnet counterparts.
/// </remarks>
public record GeneratedMavlinkMessageField : MavlinkMessageField
{
	/// <summary>
	/// The generated name of the field.
	/// </summary>
	public string GeneratedName { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneratedMavlinkMessageField"/> record with a generated name and a specific generated field type.
	/// </summary>
	/// <param name="generatedName">The generated name of the field.</param>
	/// <param name="generatedFieldType">The type of the generated field, which is a derived type of <see cref="GeneratedMavlinkMessageFieldType"/>.</param>
	/// <param name="original">The original Mavlink message field from which this instance is derived.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="generatedName"/>, <paramref name="generatedFieldType"/>, or <paramref name="original"/> is <c>null</c>.</exception>
	internal GeneratedMavlinkMessageField(string generatedName, GeneratedMavlinkMessageFieldType generatedFieldType, MavlinkMessageField original)
		: base(original)
	{
		GeneratedName = generatedName ?? throw new ArgumentNullException(nameof(generatedName));
		Type = generatedFieldType ?? throw new ArgumentNullException(nameof(generatedFieldType));
	}
}
