using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Provides a preconfigured <see cref="TemplateContext"/> for rendering in-code Scriban templates in C# code generators.
/// </summary>
public static class CSharpScribanTemplateContext
{
	private static readonly MemberRenamerDelegate PassthroughRenamer = memberInfo => memberInfo.Name;
	private static readonly MemberFilterDelegate AllowAllMembers = memberInfo => true;

	/// <summary>
	/// Creates a new <see cref="TemplateContext"/> with identity member renaming, strict variable checking,
	/// resource limits, and registered extension methods.
	/// </summary>
	public static TemplateContext Create()
	{
		var context = new TemplateContext
		{
			MemberRenamer = PassthroughRenamer,
			MemberFilter = AllowAllMembers,
			StrictVariables = true,
			EnableRelaxedMemberAccess = false,
			EnableRelaxedIndexerAccess = false,
			LoopLimit = 1000,
			RecursiveLimit = 25,
			RegexTimeOut = TimeSpan.FromSeconds(2)
		};

		var globals = new ScriptObject();
		globals.Import("to_lower_camel_case", new Func<string, string>(Utilities.ToLowerCamelCase));
		globals.Import("to_upper_camel_case", new Func<string, string>(Utilities.ToUpperCamelCase));
		globals.Import("escape_keyword", new Func<string, string>(Utilities.EscapeReservedKeyword));
		globals.Import("safe_var", new Func<string, string[], string>(Utilities.GetSafeVariableName));

		context.PushGlobal(globals);
		return context;
	}
}
