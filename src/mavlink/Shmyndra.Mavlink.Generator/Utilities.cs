using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.Generator;

internal static class Utilities
{
	public static string ToCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		// Split the input string by hyphens, underscores, or spaces
		var words = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);

		// Capitalize the first letter of each word and concatenate them
		var result = string.Concat(words.Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));

		return result;
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
