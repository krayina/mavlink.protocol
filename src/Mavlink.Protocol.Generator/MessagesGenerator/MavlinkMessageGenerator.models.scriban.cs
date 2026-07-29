namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator
{
	internal class MavlinkMessageScribanMetadata
	{
		public string Name { get; init; }
		public string OriginalName { get; init; }
		public uint Id { get; init; }
		public bool HasExtensions { get; init; }
		public List<string> AllMembers { get; init; }

		public string? SummaryCommentBlock { get; set; }
		public bool IsObsolete { get; set; }
		public string? ObsoleteMessage { get; set; }

		public MavlinkMessageScribanMetadata(string name, string originalName, uint id, bool hasExtensions, List<string> allMembers)
		{
			Name = name;
			OriginalName = originalName;
			Id = id;
			HasExtensions = hasExtensions;
			AllMembers = allMembers;
		}
	}
}
