using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// Entry point of the MAVLink source generator. Only wires the incremental pipeline;
/// parsing, resolution and emission live in their own stages.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MavlinkSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// The pipeline is added stage by stage; until then the generator emits nothing.
	}
}
