using System.Security.Cryptography;
using System.Text;

namespace Mavlink.Protocol.Generator.Parsing;

/// <summary>
/// Hash that tells whether two copies of a dialect XML are the same file: SHA-256 of the
/// UTF-8 text without BOM and with CRLF turned into LF, so git line-ending settings do not matter.
/// </summary>
internal static class XmlContentHash
{
	public static string Compute(string text)
	{
		// The BOM is not part of the text: SourceText drops it, and so does this.
		if (text.Length > 0 && text[0] == '﻿')
		{
			text = text.Substring(1);
		}

		var bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
		using var sha = SHA256.Create();
		var hash = sha.ComputeHash(bytes);

		var builder = new StringBuilder(hash.Length * 2);
		foreach (var b in hash)
		{
			builder.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
		}

		return builder.ToString();
	}
}
