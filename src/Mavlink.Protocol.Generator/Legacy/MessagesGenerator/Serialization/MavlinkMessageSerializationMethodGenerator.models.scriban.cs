using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

public partial class MavlinkMessageSerializationMethodGenerator
{
	/// <summary>
	/// The root data model for the entire serialization method Scriban template.
	/// </summary>
	internal record MavlinkSerializationMethodModel(
		string MessageName,
		string MethodSignature,
		string InitializationBlock,
		ImmutableArray<ISerializationFieldScribanTemplateModel> Fields,
		int PayloadSize
	);
}
