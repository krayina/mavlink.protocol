namespace Mavlink.Protocol.Generator;

/// <summary>
/// A context object that provides all necessary information and services
/// to an <see cref="IDeserializationFieldScribanTemplateModelFactory"/> to create a model.
/// </summary>
/// <param name="Field">The MAVLink field definition for which to create a model.</param>
/// <param name="CurrentNamespace">The current C# namespace, used for qualifying type names.</param>
/// <param name="Offset">The current byte offset within the payload for this field.</param>
/// <param name="ValidationCompiler">The service responsible for compiling validation rules into C# expressions.</param>
/// <param name="PayloadReadScribanStrategy">The service that provides platform-specific code snippets for reading data.</param>
/// <param name="UseObjectiveBitmask">A flag indicating whether to use objective-style bitmasks.</param>
public record FieldDeserializationScribanContext(
	GeneratedMavlinkMessageField Field,
	string CurrentNamespace,
	int Offset,
	IMavlinkMessageFieldValidationExpressionCompiler ValidationCompiler,
	IDeserializationPayloadReadScribanStrategy PayloadReadScribanStrategy,
	bool UseObjectiveBitmask
);
