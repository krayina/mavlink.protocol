namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator
{
	internal class MavlinkScribanModel
	{
		public string Name { get; init; }
		public string OriginalName { get; init; }
		public uint Id { get; init; }
		public bool HasExtensions { get; init; }
		public List<PropertyModel> Properties { get; init; }
		public List<string> Methods { get; init; }

		public string? Summary { get; set; }
		public bool IsObsolete { get; set; }
		public string? ObsoleteMessage { get; set; }

		public MavlinkScribanModel(string name, string originalName, uint id, bool hasExtensions, List<PropertyModel> properties, List<string> methods)
		{
			Name = name;
			OriginalName = originalName;
			Id = id;
			HasExtensions = hasExtensions;
			Properties = properties;
			Methods = methods;
		}
	}

	internal class PropertyModel
	{
		public string Declaration { get; }

		public string? Summary { get; }

		public string Remarks { get; }

		public PropertyModel(string declaration, string? summary, string remarks)
		{
			Declaration = declaration;
			Summary = summary;
			Remarks = remarks;
		}
	}
}
