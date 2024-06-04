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

	public static SyntaxTriviaList CreateRemarksTriviaIfNotNullOrEmpty(string description)
	{
		var remarksStart = SyntaxFactory.Comment("/// <remarks>");
		var remarksContent = SyntaxFactory.Comment($"/// {description}");
		var remarksEnd = SyntaxFactory.Comment("/// </remarks>");

		return SyntaxFactory.TriviaList(remarksStart, remarksContent, remarksEnd);
	}

	private static SyntaxTriviaList CreateRemarksTrivia(string description)
	{
		var remarksStart = SyntaxFactory.Comment("/// <remarks>");
		var remarksContent = SyntaxFactory.Comment($"/// {description}");
		var remarksEnd = SyntaxFactory.Comment("/// </remarks>");

		return SyntaxFactory.TriviaList(remarksStart, remarksContent, remarksEnd);
	}

	public static T AddSummaryTriviaIfNotNull<T>(this T node, string? description) where T : SyntaxNode
	{
		if (string.IsNullOrEmpty(description))
		{
			return node;
		}

		var summaryTrivia = CreateSummaryTrivia(description!);
		var existingTrivia = node.GetLeadingTrivia();

		return node.WithLeadingTrivia(existingTrivia.AddRange(summaryTrivia));
	}

	public static T AddRemarksTriviaIfNotNullOrEmpty<T>(this T node, string? originalName) where T : SyntaxNode
	{
		if (string.IsNullOrEmpty(originalName))
		{
			return node;
		}

		var remarksTrivia = CreateRemarksTrivia(originalName!);
		var existingTrivia = node.GetLeadingTrivia();

		return node.WithLeadingTrivia(existingTrivia.AddRange(remarksTrivia));
	}

	public static string GetEnumBaseType(List<ulong> values)
	{
		var maxValue = values.Max();
		if (maxValue <= byte.MaxValue) return "byte";
		if (maxValue <= ushort.MaxValue) return "ushort";
		if (maxValue <= uint.MaxValue) return "uint";
		return "ulong";
	}
}
