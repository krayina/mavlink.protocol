using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator
{
	internal sealed record PropertyTemplateModel(
		string? SummaryCommentBlock,
		string RemarksName,
		IImmutableList<string> Attributes,
		string PropertyType,
		string PropertyName);
}
