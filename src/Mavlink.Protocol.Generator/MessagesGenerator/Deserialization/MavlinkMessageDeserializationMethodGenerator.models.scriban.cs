using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageDeserializationMethodGenerator
{
	/// <summary>
	/// The root data model for the entire deserialization method Scriban template.
	/// It aggregates all information required to render a complete C# method.
	/// </summary>
	/// <param name="MessageName">The name of the MAVLink message class (e.g., "Heartbeat").</param>
	/// <param name="MethodSignature">The full C# signature of the method to be generated.</param>
	/// <param name="InitializationBlock">The C# code block for payload validation and padding.</param>
	/// <param name="Fields">An immutable array of field models, each representing a field to be deserialized.</param>
	internal record MavlinkDeserializationMethodModel(
		string MessageName,
		string MethodSignature,
		string InitializationBlock,
		ImmutableArray<IDeserializationFieldScribanTemplateModel> Fields
	);
}
