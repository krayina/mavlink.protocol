namespace Mavlink.Protocol.Generator;

public partial class MavlinkEnumGenerator
{
	internal class EnumTemplateModel
	{
		public string? SummaryCommentBlock { get; }
		public string Remarks { get; }
		public string OriginalName { get; }
		public bool IsBitmask { get; }
		public bool IsDeprecated { get; }
		public string? DeprecatedReason { get; }
		public string EnumName { get; }
		public bool HasBaseType { get; }
		public string BaseTypeName { get; }
		public List<string> Entries { get; }

		public EnumTemplateModel(
			string? summaryCommentBlock,
			string remarks,
			string originalName,
			bool isBitmask,
			bool isDeprecated,
			string? deprecatedReason,
			string enumName,
			bool hasBaseType,
			string baseTypeName,
			List<string> entries)
		{
			SummaryCommentBlock = summaryCommentBlock;
			Remarks = remarks;
			OriginalName = originalName;
			IsBitmask = isBitmask;
			IsDeprecated = isDeprecated;
			DeprecatedReason = deprecatedReason;
			EnumName = enumName;
			HasBaseType = hasBaseType;
			BaseTypeName = baseTypeName;
			Entries = entries;
		}
	}
}
