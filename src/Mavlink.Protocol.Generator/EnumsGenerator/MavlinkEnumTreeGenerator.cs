using System.Collections.Immutable;

namespace Mavlink.Protocol.Generator;

public class MavlinkEnumTreeGenerator : IMavlinkEnumTreeGenerator
{
	private readonly IMavlinkEnumGenerator _enumGenerator;

	public MavlinkEnumTreeGenerator(IMavlinkEnumGenerator enumGenerator)
	{
		_enumGenerator = enumGenerator;
	}

	public ImmutableArray<GeneratedMavlinkEnum> GenerateEnums(MavlinkNode node, string namespaceName)
	{
		var includedNamespaces = node.Includes
			.Select(includedNode => includedNode.Namespace)
			.ToImmutableArray();

		var generatedEnums = new List<GeneratedMavlinkEnum>();

		foreach (var @enum in node.Data.Enums)
		{
			var existingEnums = _enumGenerator.GetGeneratedTypes()
				.Where(e => e.Original.Name == @enum.Name && includedNamespaces.Contains(e.Namespace))
				.ToImmutableArray();

			GeneratedMavlinkEnum generatedEnum;
			if (existingEnums.IsEmpty)
			{
				generatedEnum = _enumGenerator.GenerateMavlinkEnum(@enum, namespaceName);
			}
			else
			{
				generatedEnum = _enumGenerator.GenerateAndMergeMavlinkEnum(@enum, namespaceName, existingEnums);
			}

			generatedEnums.Add(generatedEnum);
		}

		return generatedEnums.ToImmutableArray();
	}
}
