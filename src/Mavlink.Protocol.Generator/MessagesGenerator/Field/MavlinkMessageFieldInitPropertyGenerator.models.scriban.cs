using System.Collections.Immutable;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator
{
	internal sealed record PropertyTemplateModel(
		string? SummaryCommentBlock,
		string RemarksName,
		IImmutableList<string> Attributes,
		string PropertyType,
		string PropertyName);
}
