using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

internal static class Utilities
{
	/// <summary>
	/// Gets the sorted required fields and array fields (only required ones) from the provided collection.
	/// </summary>
	/// <param name="fields">An immutable array of generated message fields.</param>
	/// <returns>
	/// A tuple with two lists:
	/// <list type="bullet">
	/// <item><description>requiredFields</description></item>
	/// <item><description>arrayFields</description></item>
	/// </list>
	/// </returns>
	public static (List<GeneratedMavlinkMessageField> requiredFields, List<GeneratedMavlinkMessageField> arrayFields)
		GetSortedFields(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var requiredFields = fields
			.Where(f => f.Original.IsRequired
				&& !(f.GeneratedType is GeneratedMavlinkMessageFieldArrayType
				|| f.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
			.ToList();

		// Sort required fields by type size (largest to smallest) for proper alignment.
		requiredFields.Sort((field1, field2) =>
		{
			var size1 = GetDotNetTypeSize(field1.GeneratedType.ConvertedType);
			var size2 = GetDotNetTypeSize(field2.GeneratedType.ConvertedType);
			return size2.CompareTo(size1);
		});

		var arrayFields = fields
			.Where(f => f.Original.IsRequired
				&& (f.GeneratedType is GeneratedMavlinkMessageFieldArrayType
				|| f.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
			.ToList();

		return (requiredFields, arrayFields);
	}

	/// <summary>
	/// Escapes reserved C# keywords by prefixing them with '@' if necessary.
	/// </summary>
	/// <param name="name">The identifier name to check.</param>
	/// <returns>The escaped identifier if it is a reserved keyword; otherwise, the original name.</returns>
	public static string EscapeReservedKeyword(string name)
	{
		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
	}

	public static string GetSafeVariableName(string name, params string[] methodParameters)
	{
		if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
		{
			return "@" + name;
		}

		if (methodParameters.Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			return name + "Local";
		}

		return name;
	}

	public static string ToLowerCamelCase(string name)
	{
		return char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

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

	public static string DetermineEnumBaseType(IEnumerable<uint> values)
	{
		var maxValue = values.Max();
		if (maxValue <= byte.MaxValue) return "byte";
		if (maxValue <= ushort.MaxValue) return "ushort";
		if (maxValue <= uint.MaxValue) return "uint";
		throw new InvalidOperationException("The enum base type value cannot be greater than uint.");
	}

	public static string ToNormalizedString(this SyntaxNode syntax)
	{
		var code = syntax.ToFullString();
		code = Regex.Replace(code, @"#if(\w+)", "#if $1");
		code = Regex.Replace(code, @"#else(\w+)", "#else $1");
		code = Regex.Replace(code, @"#endif(\w+)", "#endif $1");
		code = code.Replace("? )", "?)");
		return IndentCodeWithNesting(code);
	}

	private static string IndentCodeWithNesting(string code)
	{
		var lines = code.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
		var result = new StringBuilder();
		int indentLevel = 0;
		int? ifIndentLevel = null;

		foreach (var line in lines)
		{
			var trimmedLine = line.Trim();
			if (string.IsNullOrWhiteSpace(trimmedLine))
			{
				result.AppendLine();
				continue;
			}

			if (trimmedLine.StartsWith("}"))
			{
				indentLevel = Math.Max(0, indentLevel - 1);
			}

			if (trimmedLine.StartsWith("#if"))
			{
				ifIndentLevel = indentLevel;
			}
			else if (trimmedLine.StartsWith("#else"))
			{
				if (ifIndentLevel.HasValue)
				{
					indentLevel = ifIndentLevel.Value;
				}
			}
			else if (trimmedLine.StartsWith("#endif"))
			{
				if (ifIndentLevel.HasValue)
				{
					indentLevel = ifIndentLevel.Value;
					ifIndentLevel = null;
				}
			}

			string indent = new string(' ', indentLevel * 4);
			result.AppendLine($"{indent}{trimmedLine}");

			if (trimmedLine.EndsWith("{"))
			{
				indentLevel++;
			}
		}

		return result.ToString().TrimEnd();
	}

	public static string IndentCode(string code, int indentLevel)
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

	public static void AppendWithIndent(StringBuilder sb, string content, int indentLevel)
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

	public static int CalculateMinSize(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		int minSize = 0;

		foreach (var field in fields)
		{
			int fieldSize = field.GetFieldSize();

			if (field.Original.IsRequired)
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
			.Where(f => !f.Original.IsRequired && !(f.GeneratedType is GeneratedMavlinkMessageFieldArrayType || f.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
			.Sum(f => f.GetFieldSize());
		int arrayExtensionSize = fields
			.Where(f => !f.Original.IsRequired && (f.GeneratedType is GeneratedMavlinkMessageFieldArrayType || f.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType))
			.Sum(f => f.GetFieldSize());
		return minSize + extensionLength + arrayExtensionSize;
	}

	public static int GetFieldSize(this GeneratedMavlinkMessageField field)
	{
		return field.GeneratedType switch
		{
			GeneratedMavlinkMessageFieldArrayType arrayField => GetDotNetTypeSize(arrayField.ConvertedType) * arrayField.ArrayLength,
			GeneratedMavlinkMessageFieldArrayEnumType arrayEnumField => GetDotNetTypeSize(arrayEnumField.ConvertedType) * arrayEnumField.ArrayLength,
			_ => GetDotNetTypeSize((field.GeneratedType).ConvertedType)
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

	/// <summary>
	/// Determines the combined type (byte, ushort, uint, or ulong) based on the total number of bits.
	/// </summary>
	/// <param name="totalBits">The total number of bits for the field.</param>
	/// <returns>A string representing the combined type to use for bit manipulation.</returns>
	public static string GetCombinedTypeForTotalBits(int totalBits)
	{
		if (totalBits <= 8) return "byte";
		else if (totalBits <= 16) return "ushort";
		else if (totalBits <= 32) return "uint";
		else return "ulong";
	}

	public static string GetPrimitiveBitmaskType(string typeName)
	{
		return typeName switch
		{
			"byte" => "ByteBitmask",
			"ushort" => "UShortBitmask",
			"uint" => "UIntBitmask",
			"ulong" => "ULongBitmask",
			_ => throw new NotSupportedException($"Unsupported primitive bitmask type: {typeName}")
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

	public static string GetQualifiedBitmaskTypeName(this GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return $"{enumType.GetQualifiedEnumTypeName(currentNamespace)}Bitmask";
	}

	public static string GetQualifiedBitmaskTypeName(this GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType, string currentNamespace)
	{
		return $"{arrayEnumType.GetQualifiedEnumTypeName(currentNamespace)}Bitmask";
	}

	public static string GetQualifiedEnumTypeName(this GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return enumType.GeneratedEnum.Namespace == currentNamespace
			? enumType.GeneratedEnum.GeneratedName
			: $"{enumType.GeneratedEnum.Namespace}.{enumType.GeneratedEnum.GeneratedName}";
	}

	public static string GetQualifiedEnumTypeName(this GeneratedMavlinkMessageFieldArrayEnumType enumType, string currentNamespace)
	{
		return enumType.GeneratedEnum.Namespace == currentNamespace
			? enumType.GeneratedEnum.GeneratedName
			: $"{enumType.GeneratedEnum.Namespace}.{enumType.GeneratedEnum.GeneratedName}";
	}

	public static byte CalculateCrcExtra(this IEnumerable<GeneratedMavlinkMessageField> fields, string messageName)
	{
		ushort crc = X25Crc.CrcSeed;

		crc = X25Crc.Accumulate(messageName + " ", crc);

		var sortedFields = fields.OrderByDescending(
			field => Utilities.GetDotNetTypeSize(field.GeneratedType.ConvertedType));

		foreach (var field in sortedFields)
		{
			if (!field.Original.IsRequired) continue;

			var typeName = field.Original.Type.TypeName.Equals("uint8_t_mavlink_version") ? "uint8_t" : field.Original.Type.GetTypeWithoutArray();
			crc = X25Crc.Accumulate($"{typeName} {field.Original.Name} ", crc);

			if (field.GeneratedType is GeneratedMavlinkMessageFieldArrayType arrayField)
			{
				crc = X25Crc.Accumulate(crc, (byte)arrayField.ArrayLength);
			}
			else if (field.GeneratedType is GeneratedMavlinkMessageFieldArrayEnumType arrayEnumField)
			{
				crc = X25Crc.Accumulate(crc, (byte)arrayEnumField.ArrayLength);
			}
		}

		return X25Crc.FinalizeCrc(crc);
	}
}
