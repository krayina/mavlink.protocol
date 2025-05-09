using Microsoft.CodeAnalysis;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkGenericEnumBitmaskGenerator
{
	public string Generate(GeneratedMavlinkEnum generatedEnum)
	{
		var template = Template.Parse(Templates.BitmaskTemplate);
		var context = CSharpScribanTemplateContext.Create();

		var entries = generatedEnum.GeneratedEntries
			.Where(e => e.Original.Value != 0)
			.Select(e => new ScriptObject { ["GeneratedName"] = e.GeneratedName ?? throw new InvalidOperationException("GeneratedName is null") })
			.ToArray();

		if (entries.Length > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({entries.Length}) exceeds LoopLimit ({context.LoopLimit})");
		}

		var model = new ScriptObject
		{
			["enum_name"] = generatedEnum.GeneratedName,
			["underlying_type"] = "TUnderlying",
			["enum_base_type"] = Utilities.DetermineEnumBaseType(
				generatedEnum.GeneratedEntries.Select(e => e.Original.Value)),
			["max_flags"] = entries.Length,
			["entries"] = new ScriptArray(entries),
			["mask"] = Utilities.DetermineBitmask(generatedEnum)
		};

		context.PushGlobal(model);
		try
		{
			return template.Render(context).Trim();
		}
		catch (ScriptRuntimeException ex)
		{
			throw new InvalidOperationException($"Template rendering failed. Entries count: {entries.Length}. Error: {ex.Message}", ex);
		}
		finally
		{
			context.PopGlobal();
		}
	}
}
