using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

internal static class Utilities
{
	public static ImmutableDictionary<string, (string TypeName, int Size)> MavlinkTypeMap { get; } = new Dictionary<string, (string, int)>
	{
		// MAVLink Type Name  => (C# Type Name, Size in bytes)
		{ "char",                   ("char", 1) },
		{ "uint8_t",                ("byte", 1) },
		{ "int8_t",                 ("sbyte", 1) },
		{ "uint16_t",               ("ushort", 2) },
		{ "int16_t",                ("short", 2) },
		{ "uint32_t",               ("uint", 4) },
		{ "int32_t",                ("int", 4) },
		{ "uint64_t",               ("ulong", 8) },
		{ "int64_t",                ("long", 8) },
		{ "float",                  ("float", 4) },
		{ "double",                 ("double", 8) },
    
		// The same as uint8_t
		{ "uint8_t_mavlink_version",("byte", 1) }
	}.ToImmutableDictionary();

	/// <summary>
	/// Gets the sorted required fields, partitioned into scalar and array fields.
	/// </summary>
	/// <param name="fields">An immutable array of generated message fields.</param>
	/// <returns>
	/// A tuple with two lists:
	/// <list type="bullet">
	/// <item><description>requiredScalarFields: Non-array fields, sorted by size for alignment.</description></item>
	/// <item><description>requiredArrayFields: Array fields.</description></item>
	/// </list>
	/// </returns>
	public static (List<GeneratedMavlinkMessageField> requiredScalarFields, List<GeneratedMavlinkMessageField> requiredArrayFields)
		GetSortedFields(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var requiredScalarFields = new List<GeneratedMavlinkMessageField>();
		var requiredArrayFields = new List<GeneratedMavlinkMessageField>();

		foreach (var field in fields)
		{
			if (!field.Original.IsRequired)
			{
				continue;
			}

			if (field.GeneratedType is GeneratedMavlinkMessageFieldArrayType)
			{
				requiredArrayFields.Add(field);
			}
			else
			{
				requiredScalarFields.Add(field);
			}
		}

		requiredScalarFields.Sort((field1, field2) =>
		{
			var size1 = GetDotNetTypeSize(field1.GeneratedType.GetElementTypeOrSelf().ConvertedType);
			var size2 = GetDotNetTypeSize(field2.GeneratedType.GetElementTypeOrSelf().ConvertedType);
			return size2.CompareTo(size1);
		});
		return (requiredScalarFields, requiredArrayFields);
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

	public static string PascalCaseToSnakeCase(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		return Regex.Replace(
			text,
			@"(?<!^)(?=[A-Z])",
			"_"
		).ToLowerInvariant();
	}

	public static string ToLowerCamelCase(string name)
	{
		return char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

	public static string ToUpperCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		if (IsUpperCamelCase(input))
		{
			return input;
		}

		var words = SplitIntoWords(input);

		var result = string.Concat(words.Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));

		return result;
	}

	private static bool IsUpperCamelCase(string input)
	{
		if (input.Any(c => c == '-' || c == '_' || c == ' '))
		{
			return false;
		}

		var words = SplitIntoWords(input);

		return words.All(word => char.IsUpper(word[0]));
	}

	private static List<string> SplitIntoWords(string input)
	{
		var words = new List<string>();

		if (input.Any(c => c == '-' || c == '_' || c == ' '))
		{
			return input.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries).ToList();
		}

		int start = 0;
		for (int i = 1; i < input.Length; i++)
		{
			if (char.IsUpper(input[i]))
			{
				words.Add(input.Substring(start, i - start));
				start = i;
			}
		}
		words.Add(input.Substring(start));

		return words;
	}

	public static SyntaxTriviaList CreateSummaryTrivia(string description)
	{
		var triviaNodes = new List<SyntaxTrivia>
	{
		SyntaxFactory.Comment("/// <summary>"),
		SyntaxFactory.CarriageReturnLineFeed
	};

		var lines = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
							   .Select(line => line.Trim())
							   .Where(line => !string.IsNullOrEmpty(line));

		if (lines.Any())
		{
			var contentTrivia = lines.SelectMany(line => new[]
			{
			SyntaxFactory.Comment($"/// {line}"),
			SyntaxFactory.CarriageReturnLineFeed
		});
			triviaNodes.AddRange(contentTrivia);
		}
		triviaNodes.Add(SyntaxFactory.Comment("/// </summary>"));
		triviaNodes.Add(SyntaxFactory.CarriageReturnLineFeed);

		return SyntaxFactory.TriviaList(triviaNodes);
	}

	public static SyntaxTriviaList CreateRemarksTrivia(string description)
	{
		return SyntaxFactory.TriviaList(
			SyntaxFactory.Comment("/// <remarks>"),
			SyntaxFactory.CarriageReturnLineFeed,
			SyntaxFactory.Comment($"/// {description}"),
			SyntaxFactory.CarriageReturnLineFeed,
			SyntaxFactory.Comment("/// </remarks>"),
			SyntaxFactory.CarriageReturnLineFeed
		);
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
		var attributeList = SyntaxFactory
			.AttributeList(SyntaxFactory.SingletonSeparatedList(obsoleteAttribute))
			.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

		var attributes = member.AttributeLists.Add(attributeList);

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
		if (values == null || !values.Any())
		{
			return "int";
		}

		var maxValue = values.Max();
		if (maxValue <= byte.MaxValue)
		{
			return "byte";
		}
		if (maxValue <= ushort.MaxValue)
		{
			return "ushort";
		}
		return "uint";
	}

	public static string DetermineBitmask(GeneratedMavlinkEnum generatedEnum)
	{
		var baseType = generatedEnum.GeneratedBaseType ?? DetermineEnumBaseType(
				generatedEnum.GeneratedEntries.Select(e => e.Original.Value));

		return baseType switch
		{
			"byte" => "0xFF",
			"ushort" => "0xFFFF",
			"uint" => "0xFFFFFFFF",
			"ulong" => "0xFFFFFFFFFFFFFFFF",
			_ => throw new InvalidOperationException($"Unsupported enum base type: {baseType}")
		};
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

	public static string Indent(string text)
	{
		return Indent(text, "    ");
	}

	public static string Indent(string text, string indentation = "    ")
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
		var result = new StringBuilder();

		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (!string.IsNullOrWhiteSpace(line))
			{
				result.Append(indentation);
			}

			result.Append(line);

			if (i < lines.Length - 1)
			{
				result.AppendLine();
			}
		}
		return result.ToString();
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
				result.AppendLine(trimmedLine);
			}
			else if (trimmedLine.StartsWith("#else"))
			{
				if (ifIndentLevel.HasValue)
				{
					indentLevel = ifIndentLevel.Value;
				}
				result.AppendLine(trimmedLine);
			}
			else if (trimmedLine.StartsWith("#endif"))
			{
				if (ifIndentLevel.HasValue)
				{
					indentLevel = ifIndentLevel.Value;
					ifIndentLevel = null;
				}
				result.AppendLine(trimmedLine);
			}
			else
			{
				string indent = new string(' ', indentLevel * 4);
				result.AppendLine($"{indent}{trimmedLine}");
			}

			if (trimmedLine.EndsWith("{"))
			{
				indentLevel++;
			}
		}

		return result.ToString().TrimEnd();
	}

	public static string IndentCode(string code, int indentLevel)
	{
		if (string.IsNullOrEmpty(code))
		{
			return string.Empty;
		}

		var indent = new string(' ', indentLevel * 4);
		var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		var sb = new StringBuilder();

		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (!string.IsNullOrWhiteSpace(line))
			{
				sb.Append(indent);
			}

			sb.Append(line);
			if (i < lines.Length - 1)
			{
				sb.Append('\n');
			}
		}

		return sb.ToString();
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
			int fieldSize = field.GeneratedType.GetFieldTypeSize();

			if (field.Original.IsRequired)
			{
				minSize += fieldSize;
			}
		}

		return minSize;
	}

	public static int CalculateFinalSize(this ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		int requiredSize = fields
			.Where(f => f.Original.IsRequired)
			.Sum(f => f.GeneratedType.GetFieldTypeSize());

		int extensionSize = fields
			.Where(f => !f.Original.IsRequired)
			.Sum(f => f.GeneratedType.GetFieldTypeSize());

		return requiredSize + extensionSize;
	}

	public static int GetFieldTypeSize(this GeneratedMavlinkMessageFieldType type)
	{
		return type switch
		{
			GeneratedMavlinkMessageFieldArrayType arrayType =>
				GetDotNetTypeSize(arrayType.ConvertedType) * arrayType.ArrayLength,

			GeneratedMavlinkMessageFieldScalarType scalarType =>
				GetDotNetTypeSize(scalarType.ConvertedType),

			_ => throw new NotSupportedException($"Cannot determine field size for type {type.GetType().Name}")
		};
	}

	/// <summary>
	/// Determines the bitmask for excess bits in underlyingType that are outside the range of enumBaseType.
	/// </summary>
	/// <param name="underlyingType">The type of the bitmask (e.g., ushort, uint).</param>
	/// <param name="enumBaseType">The base type of the enum (e.g., byte, ushort).</param>
	/// <returns>A hexadecimal string representing the mask for excess bits, or "0x0" if no excess bits exist.</returns>
	public static string DetermineExcessBitsMask(string underlyingType, string enumBaseType)
	{
		int underlyingTypeBits = GetDotNetTypeSize(underlyingType) * 8;
		int enumBaseTypeBits = GetDotNetTypeSize(enumBaseType) * 8;

		if (underlyingTypeBits <= enumBaseTypeBits)
		{
			return "0x0";
		}

		long fullMask = (1L << underlyingTypeBits) - 1;
		long enumMask = (1L << enumBaseTypeBits) - 1;
		long excessMask = fullMask & ~enumMask;

		return $"0x{excessMask:X}";
	}

	public static int GetDotNetTypeSize(string convertedType)
	{
		return convertedType switch
		{
			"byte" => 1,
			"sbyte" => 1,
			"char" => 2,
			"ushort" => 2,
			"short" => 2,
			"uint" => 4,
			"int" => 4,
			"float" => 4,
			"ulong" => 8,
			"long" => 8,
			"double" => 8,
			_ => throw new NotSupportedException($"Unsupported type: {convertedType}"),
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

	/// <summary>
	/// Gets the fully qualified name of the generated enum, including its namespace,
	/// unless it resides in the same namespace as the referencing type.
	/// </summary>
	/// <param name="generatedEnum">The generated enum for which to get the name.</param>
	/// <param name="referencingNamespace">The namespace of the code that will be referencing this enum (e.g., the message's namespace).</param>
	/// <returns>
	/// The simple name of the enum if it's in the same namespace, 
	/// or the fully qualified name (e.g., "MyProject.Enums.MyEnum") otherwise.
	/// </returns>
	public static string GetQualifiedName(this GeneratedMavlinkEnum generatedEnum, string referencingNamespace)
	{
		if (string.IsNullOrEmpty(referencingNamespace) || generatedEnum.Namespace == referencingNamespace)
		{
			return generatedEnum.GeneratedName;
		}
		else
		{
			return $"{generatedEnum.Namespace}.{generatedEnum.GeneratedName}";
		}
	}

	public static string GetQualifiedBitmaskTypeName(this GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
	{
		return $"{enumType.GetQualifiedEnumTypeName(currentNamespace)}Bitmask";
	}

	public static string GetQualifiedEnumTypeName(this GeneratedMavlinkMessageFieldEnumType enumType, string currentNamespace)
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

			if (field.GeneratedType is GeneratedMavlinkMessageFieldArrayType arrayType)
			{
				crc = X25Crc.Accumulate(crc, (byte)arrayType.ArrayLength);
			}
		}

		return X25Crc.FinalizeCrc(crc);
	}
}
