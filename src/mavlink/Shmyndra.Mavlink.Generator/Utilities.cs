using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

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

	public static string DetermineEnumBaseType(IEnumerable<uint> values)
	{
		var maxValue = values.Max();
		if (maxValue <= byte.MaxValue) return "byte";
		if (maxValue <= ushort.MaxValue) return "ushort";
		if (maxValue <= uint.MaxValue) return "uint";
		throw new InvalidOperationException("The enum base type value cannot be greater than uint.");
	}

	public static PropertyDeclarationSyntax AddArrayLengthAttribute(this PropertyDeclarationSyntax property, int length)
	{
		return property.AddAttributeLists(
			SyntaxFactory.AttributeList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.Attribute(
						SyntaxFactory.ParseName(typeof(System.ComponentModel.DataAnnotations.RequiredArrayLengthAttribute).FullName
							.GetAttributeNameWithoutPostfix()
						)
					)
					.WithArgumentList(
						SyntaxFactory.AttributeArgumentList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(length))))
						)
					)
				)
			)
		);
	}

	public static string GetAttributeNameWithoutPostfix(this string attributeName)
	{
		const string postfix = "Attribute";
		if (attributeName.EndsWith(postfix, StringComparison.Ordinal))
		{
			return attributeName.Substring(0, attributeName.Length - postfix.Length);
		}
		return attributeName;
	}

	public static T AddObsoleteAttribute<T>(this T member, string? obsoleteMessage) where T : MemberDeclarationSyntax
	{
		if (obsoleteMessage is null)
		{
			return member;
		}

		var obsoleteAttribute = CreateObsoleteAttribute(obsoleteMessage);
		var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(obsoleteAttribute));
		var attributes = member.AttributeLists.Add(attributeList);

		// Ensure the cast back to the original type T
		return (T)member.WithAttributeLists(attributes);
	}

	public static AttributeSyntax CreateObsoleteAttribute(string message)
	{
		return SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Obsolete"))
			.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
				SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(message)))
			)));
	}

	public static IImmutableDictionary<TKey, TValue> ToImmutableIndexSortedDictionary<TKey, TValue>(
		this Dictionary<TKey, (int Index, TValue Value)> dictionary) where TKey : notnull
	{
		return dictionary
			.ToImmutableSortedDictionary(
				kv => kv.Key,
				kv => kv.Value.Value,
				Comparer<TKey>.Create((x, y) =>
					dictionary[x].Index.CompareTo(dictionary[y].Index))
			);
	}

	public static string ToNormalizedString(this SyntaxNode syntax)
	{
		return syntax.NormalizeWhitespace().ToFullString().Replace("? )", "?)");
	}

	public static int CalculateMinSize(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		int minSize = 0;

		foreach (var field in fields)
		{
			int fieldSize = field.GetFieldSize();

			if (field.IsRequired)
			{
				minSize += fieldSize;
			}
		}

		return minSize;
	}

	public static int CalculateFinalSize(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		int minSize = fields.CalculateMinSize();
		int extensionLength = fields
			.Where(f => !f.IsRequired && !(f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType))
			.Sum(f => f.GetFieldSize());
		int arrayExtensionSize = fields
			.Where(f => !f.IsRequired && (f.Type is GeneratedMavlinkMessageFieldArrayType || f.Type is GeneratedMavlinkMessageFieldArrayEnumType))
			.Sum(f => f.GetFieldSize());
		return minSize + extensionLength + arrayExtensionSize;
	}

	public static int GetFieldSize(this GeneratedMavlinkMessageField field)
	{
		return field.Type switch
		{
			GeneratedMavlinkMessageFieldArrayType arrayField => Utilities.GetDotNetTypeSize(arrayField.ConvertedType) * arrayField.ArrayLength,
			GeneratedMavlinkMessageFieldArrayEnumType arrayEnumField => Utilities.GetDotNetTypeSize(arrayEnumField.ConvertedType) * arrayEnumField.ArrayLength,
			_ => GetDotNetTypeSize(((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType)
		};
	}

	public static int GetDotNetTypeSize(string convertedType)
	{
		return convertedType switch
		{
			"byte" => 1,
			"sbyte" => 1,
			"char" => 1,
			"ushort" => 2,
			"short" => 2,
			"uint" => 4,
			"int" => 4,
			"float" => 4,
			"ulong" => 8,
			"long" => 8,
			"double" => 8,
			_ => throw new InvalidOperationException($"Unknown type: {convertedType}"),
		};
	}

	public static string GetTypeWithoutArray(this MavlinkMessageFieldType type)
	{
		string typeName = type.TypeName;
		int arrayStartIndex = typeName.IndexOf('[');
		if (arrayStartIndex >= 0)
		{
			typeName = typeName.Substring(0, arrayStartIndex);
		}
		return typeName;
	}

	public static byte CalculateCrcExtra(this IEnumerable<GeneratedMavlinkMessageField> fields, string messageName)
	{
		ushort crc = X25Crc.CrcSeed;

		crc = X25Crc.Accumulate(messageName + " ", crc);

		var sortedFields = fields.OrderByDescending(
			field => Utilities.GetDotNetTypeSize(((GeneratedMavlinkMessageFieldType)field.Type).ConvertedType));

		foreach (var field in sortedFields)
		{
			if (!field.IsRequired) continue;

			var typeName = field.Type.TypeName.Equals("uint8_t_mavlink_version") ? "uint8_t" : field.Type.GetTypeWithoutArray();
			crc = X25Crc.Accumulate($"{typeName} {field.Name} ", crc);

			if (field.Type is GeneratedMavlinkMessageFieldArrayType arrayField)
			{
				crc = X25Crc.Accumulate(crc, (byte)arrayField.ArrayLength);
			}
			else if (field.Type is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumField)
			{
				crc = X25Crc.Accumulate(crc, (byte)arrayEnumField.ArrayLength);
			}
		}

		return X25Crc.FinalizeCrc(crc);
	}
}
