using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkSpecificEnumBitmaskGenerator
{
	private static readonly string[] SupportedTypes = { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong" };

	public MemberDeclarationSyntax Generate(GeneratedMavlinkEnum generatedEnum, string underlyingType)
	{
		if (!SupportedTypes.Contains(underlyingType))
		{
			throw new InvalidOperationException($"Unsupported underlying type '{underlyingType}' for enum '{generatedEnum.GeneratedName}'.");
		}

		long validMask = 0;
		foreach (var entry in generatedEnum.GeneratedEntries)
		{
			validMask |= entry.Original.Value;
		}

		bool isValidType = underlyingType switch
		{
			"byte" => validMask >= 0 && validMask <= byte.MaxValue,
			"sbyte" => validMask >= sbyte.MinValue && validMask <= sbyte.MaxValue,
			"short" => validMask >= short.MinValue && validMask <= short.MaxValue,
			"ushort" => validMask >= 0 && validMask <= ushort.MaxValue,
			"int" => validMask >= int.MinValue && validMask <= int.MaxValue,
			"uint" => validMask >= 0 && validMask <= uint.MaxValue,
			"long" => validMask >= long.MinValue && validMask <= long.MaxValue,
			"ulong" => validMask >= 0,
			_ => false
		};

		if (!isValidType)
		{
			throw new InvalidOperationException($"Underlying type '{underlyingType}' cannot hold all values of enum '{generatedEnum.GeneratedName}'. ValidMask: 0x{validMask:X}");
		}

		string enumName = generatedEnum.GeneratedName;
		string enumBaseType = Utilities.DetermineEnumBaseType(generatedEnum.GeneratedEntries.Select(e => e.Original.Value));
		string structName = $"{enumName}{Utilities.ToCamelCase(underlyingType)}Bitmask";

		var entries = generatedEnum.GeneratedEntries
			.Where(e => e.Original.Value != 0)
			.Select(e => new ScriptObject { ["GeneratedName"] = e.GeneratedName ?? throw new InvalidOperationException("GeneratedName is null") })
			.ToArray();

		string validMaskHex = $"0x{validMask:X}";
		int enumValuesCount = generatedEnum.GeneratedEntries.Length;

		var template = Template.Parse(Templates.SpecificBitmaskTemplate);
		var context = CSharpScribanTemplateContext.Create();

		if (entries.Length > context.LoopLimit)
		{
			throw new InvalidOperationException($"Entries count ({entries.Length}) exceeds LoopLimit ({context.LoopLimit})");
		}

		var model = new ScriptObject
		{
			["namespace"] = generatedEnum.Namespace,
			["struct_name"] = structName,
			["enum_name"] = enumName,
			["underlying_type"] = underlyingType,
			["enum_base_type"] = enumBaseType,
			["entries"] = new ScriptArray(entries),
			["valid_mask"] = validMaskHex,
			["enum_values_count"] = enumValuesCount
		};

		context.PushGlobal(model);
		try
		{
			string rendered = template.Render(context).Trim();
			var syntax = SyntaxFactory.ParseMemberDeclaration(rendered);
			if (syntax == null)
			{
				throw new InvalidOperationException($"Failed to parse the generated struct '{structName}' for enum '{enumName}' with underlying type '{underlyingType}'. Generated code:\n{rendered}");
			}
			return syntax;
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
