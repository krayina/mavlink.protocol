using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.SourceGenerators;

internal static class Utilities
{
	public static string ToCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		var words = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < words.Length; i++)
		{
			words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
		}

		return string.Join("", words);
	}

	public static SyntaxTriviaList CreateSummaryTrivia(string description)
	{
		var summaryStart = SyntaxFactory.Comment("/// <summary>");
		var summaryContent = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
										.Select(line => SyntaxFactory.Comment($"/// {line.Trim()}"));
		var summaryEnd = SyntaxFactory.Comment("/// </summary>");

		return SyntaxFactory.TriviaList(summaryStart)
							.AddRange(summaryContent)
							.Add(summaryEnd);
	}

	public static string GetEnumBaseType(List<ulong> values)
	{
		var maxValue = values.Max();
		if (maxValue <= byte.MaxValue) return "byte";
		if (maxValue <= ushort.MaxValue) return "ushort";
		if (maxValue <= uint.MaxValue) return "uint";
		return "ulong";
	}

	public static T AddSummaryTriviaIfNotNull<T>(T node, string? description) where T : SyntaxNode
	{
		if (string.IsNullOrEmpty(description))
		{
			return node;
		}

		return node.WithLeadingTrivia(CreateSummaryTrivia(description!));
	}
}
