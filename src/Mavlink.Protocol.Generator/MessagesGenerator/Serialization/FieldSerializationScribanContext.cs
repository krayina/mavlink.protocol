namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// A context object that provides all necessary information and services
/// to an <see cref="ISerializationFieldScribanTemplateModelFactory"/> to create a model.
/// This is the serialization counterpart to <c>FieldDeserializationScribanContext</c>.
/// </summary>
/// <param name="Field">The MAVLink field definition for which to create a model.</param>
/// <param name="Offset">The current byte offset within the payload for this field.</param>
/// <param name="PayloadWriteScribanStrategy">The service that provides platform-specific code snippets for writing data.</param>
/// <param name="UseObjectiveBitmask">A flag indicating whether to use objective-style bitmasks.</param>
/// <param name="InvalidValueBuilder">A service to build a C# expression for a field's 'invalid' value, used for serializing nulls.</param>
public record FieldSerializationScribanContext(
	GeneratedMavlinkMessageField Field,
	int Offset,
	ISerializationPayloadWriteScribanStrategy PayloadWriteScribanStrategy,
	bool UseObjectiveBitmask,
	IInvalidValueExpressionBuilder InvalidValueBuilder
);
