using System.Text;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkEnumBitmaskGenerator
{
	public static string GenerateEnumBitmaskTypes(IEnumerable<GeneratedMavlinkEnum> enums)
	{
		var sb = new StringBuilder();
		sb.AppendLine("namespace MavlinkTypes");
		sb.AppendLine("{");

		foreach (var mavlinkEnum in enums)
		{
			string code = GenerateBitmaskType(mavlinkEnum);
			if (!string.IsNullOrEmpty(code))
			{
				sb.AppendLine(IndentCode(code, 1));
			}
		}

		sb.AppendLine("}");
		return sb.ToString();
	}

	public static string GenerateBitmaskType(GeneratedMavlinkEnum mavlinkEnum)
	{
		if (mavlinkEnum.Original.Bitmask == false)
		{
			return string.Empty;
		}

		string enumName = mavlinkEnum.GeneratedName;
		string underlyingType = DetermineUnderlyingType(mavlinkEnum);
		var sb = new StringBuilder();

		AppendWithIndent(sb, GenerateHeader(enumName, underlyingType), 0);
		AppendWithIndent(sb, GenerateFields(enumName, underlyingType), 1);
		AppendWithIndent(sb, GenerateConstructor(enumName, underlyingType), 1);
		AppendWithIndent(sb, GenerateProperties(enumName, underlyingType), 1);
		AppendWithIndent(sb, GenerateComputeActiveFlags(enumName, underlyingType, mavlinkEnum), 1);
		AppendWithIndent(sb, GenerateEqualityMethods(enumName, underlyingType), 1);
		AppendWithIndent(sb, GenerateToString(), 1);
		AppendWithIndent(sb, "}", 0);

		return sb.ToString();
	}

	private static void AppendWithIndent(StringBuilder sb, string content, int indentLevel)
	{
		string indent = new string(' ', indentLevel * 4);
		foreach (var line in content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None))
		{
			if (!string.IsNullOrWhiteSpace(line))
			{
				sb.AppendLine($"{indent}{line.TrimEnd()}");
			}
			else
			{
				sb.AppendLine();
			}
		}
	}

	private static string IndentCode(string code, int indentLevel)
	{
		string indent = new string(' ', indentLevel * 4);
		var sb = new StringBuilder();
		foreach (var line in code.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None))
		{
			if (!string.IsNullOrWhiteSpace(line))
			{
				sb.AppendLine($"{indent}{line.TrimEnd()}");
			}
			else
			{
				sb.AppendLine();
			}
		}
		return sb.ToString().TrimEnd();
	}

	private static string GenerateHeader(string enumName, string underlyingType)
	{
		return $$"""
#if NET8_0_OR_GREATER
public readonly struct {{enumName}}Bitmask : IEnumBitmask<{{enumName}}, {{underlyingType}}>
    where {{underlyingType}} : struct, System.Numerics.IBinaryInteger<{{underlyingType}}>
#else
public readonly struct {{enumName}}Bitmask : IEnumBitmask<{{enumName}}, {{underlyingType}}>
    where {{underlyingType}} : struct
#endif
{
""";
	}

	private static string GenerateFields(string enumName, string underlyingType)
	{
		return $$"""
private readonly {{underlyingType}} _bitmask;
private readonly System.Collections.Immutable.ImmutableArray<{{enumName}}> _activeFlags;
""";
	}

	private static string GenerateConstructor(string enumName, string underlyingType)
	{
		return $$"""
public {{enumName}}Bitmask({{underlyingType}} bitmask)
{
    _bitmask = bitmask;
    _activeFlags = default;
}
""";
	}

	private static string GenerateProperties(string enumName, string underlyingType)
	{
		return $$"""
public {{underlyingType}} Bitmask => _bitmask;

public {{enumName}} Value
{
    get
    {
#if NET8_0_OR_GREATER
        var maskedValue = _bitmask & {{underlyingType}}.CreateTruncating(0xFF);
        return ({{enumName}})byte.CreateTruncating(maskedValue);
#else
        return _bitmask switch
        {
            byte b => ({{enumName}})b,
            ushort us => ({{enumName}})(byte)us,
            uint ui => ({{enumName}})(byte)ui,
            ulong ul => ({{enumName}})(byte)ul,
            _ => throw new System.InvalidOperationException("Unsupported underlying type")
        };
#endif
    }
}

public System.Collections.Immutable.ImmutableArray<{{enumName}}> ActiveFlags => _activeFlags.IsDefault ? ComputeActiveFlags() : _activeFlags;
""";
	}

	private static string GenerateComputeActiveFlags(string enumName, string underlyingType, GeneratedMavlinkEnum mavlinkEnum)
	{
		var flagChecksNet8 = GenerateFlagChecksNet8(mavlinkEnum, underlyingType);
		var flagChecksLegacy = GenerateFlagChecksLegacy(mavlinkEnum);
		int maxFlags = mavlinkEnum.GeneratedEntries.Count(e => e.Original.Value != 0);

		return $$"""
private System.Collections.Immutable.ImmutableArray<{{enumName}}> ComputeActiveFlags()
{
#if NET8_0_OR_GREATER
    if (_bitmask == {{underlyingType}}.Zero)
    {
        return System.Collections.Immutable.ImmutableArray<{{enumName}}>.Empty;
    }

    {{underlyingType}} maskedBitmask = _bitmask & {{underlyingType}}.CreateTruncating(0xFF);
    int count = int.CreateTruncating({{underlyingType}}.PopCount(maskedBitmask));
    var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{enumName}}>(count);
{{flagChecksNet8}}
    return builder.MoveToImmutable();
#else
    if (System.Collections.Generic.EqualityComparer<{{underlyingType}}>.Default.Equals(_bitmask, default))
    {
        return System.Collections.Immutable.ImmutableArray<{{enumName}}>.Empty;
    }

    byte maskedBitmask = _bitmask switch
    {
        byte b => b,
        ushort us => (byte)us,
        uint ui => (byte)ui,
        ulong ul => (byte)ul,
        _ => throw new System.InvalidOperationException("Unsupported underlying type")
    };

    var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{enumName}}>({{maxFlags}});
{{flagChecksLegacy}}
    return builder.MoveToImmutable();
#endif
}
""";
	}

	private static string GenerateFlagChecksNet8(GeneratedMavlinkEnum mavlinkEnum, string underlyingType)
	{
		var checks = mavlinkEnum.GeneratedEntries
			.Where(e => e.Original.Value != 0)
			.Select(e => $$"""
    if ((maskedBitmask & {{underlyingType}}.CreateTruncating((byte){{mavlinkEnum.GeneratedName}}.{{e.GeneratedName}})) != {{underlyingType}}.Zero)
    {
        builder.Add({{mavlinkEnum.GeneratedName}}.{{e.GeneratedName}});
    }
""")
			.ToArray();

		return string.Join("", checks);
	}

	private static string GenerateFlagChecksLegacy(GeneratedMavlinkEnum mavlinkEnum)
	{
		var checks = mavlinkEnum.GeneratedEntries
			.Where(e => e.Original.Value != 0)
			.Select(e => $$"""
    if ((maskedBitmask & (byte){{mavlinkEnum.GeneratedName}}.{{e.GeneratedName}}) != 0)
    {
        builder.Add({{mavlinkEnum.GeneratedName}}.{{e.GeneratedName}});
    }
""")
			.ToArray();

		return string.Join("", checks);
	}

	private static string GenerateEqualityMethods(string enumName, string underlyingType)
	{
		return $$"""
public override bool Equals(object? obj) => obj is {{enumName}}Bitmask other && Equals(other);
public bool Equals({{enumName}}Bitmask other) => System.Collections.Generic.EqualityComparer<{{underlyingType}}>.Default.Equals(_bitmask, other._bitmask);
public override int GetHashCode() => _bitmask.GetHashCode();
public static bool operator ==({{enumName}}Bitmask left, {{enumName}}Bitmask right) => left.Equals(right);
public static bool operator !=({{enumName}}Bitmask left, {{enumName}}Bitmask right) => !left.Equals(right);
""";
	}

	private static string GenerateToString()
	{
		return """
public override string ToString() => $"Bitmask: {_bitmask} (Active flags: {string.Join(", ", ActiveFlags)})";
""";
	}

	private static string DetermineUnderlyingType(GeneratedMavlinkEnum mavlinkEnum)
	{
		uint maxValue = mavlinkEnum.GeneratedEntries.Max(e => e.Original.Value);
		if (maxValue <= byte.MaxValue)
		{
			return "byte";
		}
		else if (maxValue <= ushort.MaxValue)
		{
			return "ushort";
		}
		else if (maxValue <= uint.MaxValue)
		{
			return "uint";
		}
		else
		{
			return "ulong";
		}
	}
}
