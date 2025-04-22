using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

public class MavlinkGenericEnumBitmaskGenerator
{
	public MemberDeclarationSyntax Generate(GeneratedMavlinkEnum generatedEnum)
	{
		string enumName = generatedEnum.GeneratedName;
		var underlyingType = generatedEnum.GeneratedBaseType ?? Utilities.DetermineEnumBaseType(generatedEnum.GeneratedEntries.Select(e => e.Original.Value));
		string structName = $"{enumName}Bitmask";

		var sb = new StringBuilder();
		sb.AppendLine($"#if NET8_0_OR_GREATER");
		Utilities.AppendWithIndent(sb, $"public readonly struct {structName} : IEnumBitmask<{enumName}, {underlyingType}>", 0);
		Utilities.AppendWithIndent(sb, $"where {underlyingType} : struct, System.Numerics.IBinaryInteger<{underlyingType}>", 1);
		sb.AppendLine($"#else");
		Utilities.AppendWithIndent(sb, $"public readonly struct {structName} : IEnumBitmask<{enumName}, {underlyingType}>", 0);
		Utilities.AppendWithIndent(sb, $"where {underlyingType} : struct", 1);
		sb.AppendLine($"#endif");
		Utilities.AppendWithIndent(sb, "{", 0);

		AppendFields(sb, enumName, underlyingType);
		AppendConstructor(sb, structName, underlyingType);
		AppendProperties(sb, enumName, underlyingType, generatedEnum);
		AppendEqualityMethods(sb, structName, underlyingType);
		AppendToString(sb);

		Utilities.AppendWithIndent(sb, "}", 0);

		var result = SyntaxFactory.ParseMemberDeclaration(sb.ToString());
		if (result == null)
		{
			throw new InvalidOperationException(
				$"Failed to parse the generated struct '{structName}' for enum '{enumName}'. " +
				$"Generated code:\n{sb}\n" +
				"Possible causes: Invalid syntax in the generated code or unsupported C# constructs."
			);
		}

		return result;
	}

	private void AppendFields(StringBuilder sb, string enumName, string underlyingType)
	{
		Utilities.AppendWithIndent(sb, $"private readonly {underlyingType} _bitmask;", 1);
		Utilities.AppendWithIndent(sb, $"private readonly System.Collections.Immutable.ImmutableArray<{enumName}> _activeFlags;", 1);
	}

	private void AppendConstructor(StringBuilder sb, string structName, string underlyingType)
	{
		Utilities.AppendWithIndent(sb, $"public {structName}({underlyingType} bitmask)", 1);
		Utilities.AppendWithIndent(sb, "{", 1);
		Utilities.AppendWithIndent(sb, "_bitmask = bitmask;", 2);
		Utilities.AppendWithIndent(sb, "_activeFlags = default;", 2);
		Utilities.AppendWithIndent(sb, "}", 1);
	}

	private void AppendProperties(StringBuilder sb, string enumName, string underlyingType, GeneratedMavlinkEnum generatedEnum)
	{
		Utilities.AppendWithIndent(sb, $"public {underlyingType} Bitmask => _bitmask;", 1);
		Utilities.AppendWithIndent(sb, $"public {enumName} Value", 1);
		Utilities.AppendWithIndent(sb, "{", 1);
		Utilities.AppendWithIndent(sb, "get", 2);
		Utilities.AppendWithIndent(sb, "{", 2);
		sb.AppendLine($"#if NET8_0_OR_GREATER");
		Utilities.AppendWithIndent(sb, $"var maskedValue = _bitmask & {underlyingType}.CreateTruncating(0xFF);", 3);
		Utilities.AppendWithIndent(sb, $"return ({enumName})byte.CreateTruncating(maskedValue);", 3);
		sb.AppendLine($"#else");
		Utilities.AppendWithIndent(sb, $"return ({enumName})_bitmask;", 3);
		sb.AppendLine($"#endif");
		Utilities.AppendWithIndent(sb, "}", 2);
		Utilities.AppendWithIndent(sb, "}", 1);
		Utilities.AppendWithIndent(sb, $"public System.Collections.Immutable.ImmutableArray<{enumName}> ActiveFlags => _activeFlags.IsDefault ? ComputeActiveFlags() : _activeFlags;", 1);
		AppendComputeActiveFlags(sb, enumName, underlyingType, generatedEnum);
	}

	private void AppendComputeActiveFlags(StringBuilder sb, string enumName, string underlyingType, GeneratedMavlinkEnum generatedEnum)
	{
		int maxFlags = generatedEnum.GeneratedEntries.Count(e => e.Original.Value != 0);
		Utilities.AppendWithIndent(sb, $"private System.Collections.Immutable.ImmutableArray<{enumName}> ComputeActiveFlags()", 1);
		Utilities.AppendWithIndent(sb, "{", 1);
		sb.AppendLine($"#if NET8_0_OR_GREATER");
		Utilities.AppendWithIndent(sb, $"if (_bitmask == {underlyingType}.Zero)", 2);
		Utilities.AppendWithIndent(sb, "{", 2);
		Utilities.AppendWithIndent(sb, $"return System.Collections.Immutable.ImmutableArray<{enumName}>.Empty;", 3);
		Utilities.AppendWithIndent(sb, "}", 2);
		Utilities.AppendWithIndent(sb, $"byte maskedBitmask = _bitmask & {underlyingType}.CreateTruncating(0xFF);", 2);
		Utilities.AppendWithIndent(sb, $"int count = int.CreateTruncating({underlyingType}.PopCount(maskedBitmask));", 2);
		Utilities.AppendWithIndent(sb, $"var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{enumName}>(count);", 2);
		foreach (var entry in generatedEnum.GeneratedEntries.Where(e => e.Original.Value != 0))
		{
			Utilities.AppendWithIndent(sb, $"if ((maskedBitmask & {underlyingType}.CreateTruncating((byte){enumName}.{entry.GeneratedName})) != {underlyingType}.Zero)", 2);
			Utilities.AppendWithIndent(sb, "{", 2);
			Utilities.AppendWithIndent(sb, $"builder.Add({enumName}.{entry.GeneratedName});", 3);
			Utilities.AppendWithIndent(sb, "}", 2);
		}
		Utilities.AppendWithIndent(sb, $"return builder.MoveToImmutable();", 2);
		sb.AppendLine($"#else");
		Utilities.AppendWithIndent(sb, $"if (System.Collections.Generic.EqualityComparer<{underlyingType}>.Default.Equals(_bitmask, default))", 2);
		Utilities.AppendWithIndent(sb, "{", 2);
		Utilities.AppendWithIndent(sb, $"return System.Collections.Immutable.ImmutableArray<{enumName}>.Empty;", 3);
		Utilities.AppendWithIndent(sb, "}", 2);
		Utilities.AppendWithIndent(sb, $"byte maskedBitmask = _bitmask;", 2);
		Utilities.AppendWithIndent(sb, $"var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{enumName}>({maxFlags});", 2);
		foreach (var entry in generatedEnum.GeneratedEntries.Where(e => e.Original.Value != 0))
		{
			Utilities.AppendWithIndent(sb, $"if ((maskedBitmask & (byte){enumName}.{entry.GeneratedName}) != 0)", 2);
			Utilities.AppendWithIndent(sb, "{", 2);
			Utilities.AppendWithIndent(sb, $"builder.Add({enumName}.{entry.GeneratedName});", 3);
			Utilities.AppendWithIndent(sb, "}", 2);
		}
		Utilities.AppendWithIndent(sb, $"return builder.MoveToImmutable();", 2);
		sb.AppendLine($"#endif");
		Utilities.AppendWithIndent(sb, "}", 1);
	}

	private void AppendEqualityMethods(StringBuilder sb, string structName, string underlyingType)
	{
		Utilities.AppendWithIndent(sb, $"public override bool Equals(object? obj) => obj is {structName} other && Equals(other);", 1);
		Utilities.AppendWithIndent(sb, $"public bool Equals({structName} other) => System.Collections.Generic.EqualityComparer<{underlyingType}>.Default.Equals(_bitmask, other._bitmask);", 1);
		Utilities.AppendWithIndent(sb, $"public override int GetHashCode() => _bitmask.GetHashCode();", 1);
		Utilities.AppendWithIndent(sb, $"public static bool operator ==({structName} left, {structName} right) => left.Equals(right);", 1);
		Utilities.AppendWithIndent(sb, $"public static bool operator !=({structName} left, {structName} right) => !left.Equals(right);", 1);
	}

	private void AppendToString(StringBuilder sb)
	{
		Utilities.AppendWithIndent(sb, $"public override string ToString() => $\"Bitmask: {{_bitmask}}, Active flags: [{{string.Join(\", \", ActiveFlags)}}]\";", 1);
	}
}
