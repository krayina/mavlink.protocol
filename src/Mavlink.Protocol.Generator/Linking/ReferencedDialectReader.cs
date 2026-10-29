using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;
using Microsoft.CodeAnalysis;

namespace Mavlink.Protocol.Generator.Linking;

/// <summary>
/// Reads the dialects of referenced assemblies from their metadata: one
/// <c>[assembly: MavlinkDialectManifest]</c> per dialect and <c>[MavlinkEnum]</c> on its enums.
/// The XML of those dialects is not needed.
/// </summary>
internal static class ReferencedDialectReader
{
	/// <summary>
	/// The manifest format this generator writes and reads.
	/// </summary>
	public const int FormatVersion = 1;

	private const string ManifestAttributeName = "Mavlink.Dialects.Infrastructure.MavlinkDialectManifestAttribute";
	private const string EnumAttributeName = "Mavlink.Dialects.Infrastructure.MavlinkEnumAttribute";

	public static ReferencedDialects Read(Compilation compilation, CancellationToken cancellationToken)
	{
		var manifestAttribute = compilation.GetTypeByMetadataName(ManifestAttributeName);
		var enumAttribute = compilation.GetTypeByMetadataName(EnumAttributeName);
		if (manifestAttribute is null || enumAttribute is null)
		{
			return ReferencedDialects.Empty;
		}

		var coreName = manifestAttribute.ContainingAssembly.Name;
		var diagnostics = new List<DiagnosticInfo>();
		var dialects = new List<ReferencedDialect>();
		var byName = new Dictionary<string, ReferencedDialect>(DialectName.Comparer);

		// Sorted so that which duplicate wins does not depend on reference order.
		foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.OrderBy(a => a.Name, StringComparer.Ordinal))
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Only an assembly built against Core can carry manifests; this skips the framework.
			if (!assembly.Modules.Any(m => m.ReferencedAssemblies.Any(r => r.Name == coreName)))
			{
				continue;
			}

			var manifests = assembly.GetAttributes()
				.Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, manifestAttribute))
				.Select(a => ReadManifest(a, assembly, enumAttribute, diagnostics))
				.OfType<ReferencedDialect>()
				.OrderBy(d => d.Name, StringComparer.Ordinal);

			foreach (var dialect in manifests)
			{
				if (byName.TryGetValue(dialect.Name, out var first))
				{
					diagnostics.Add(DiagnosticInfo.Create(
						DiagnosticDescriptors.DuplicateReferencedDialect,
						null,
						dialect.Name,
						first.Assembly,
						dialect.Assembly));
					continue;
				}

				byName.Add(dialect.Name, dialect);
				dialects.Add(dialect);
			}
		}

		foreach (var dialect in dialects)
		{
			foreach (var include in dialect.Includes)
			{
				if (!byName.ContainsKey(include))
				{
					diagnostics.Add(DiagnosticInfo.Create(
						DiagnosticDescriptors.ReferencedIncludeMissing,
						null,
						dialect.Name,
						dialect.Assembly,
						include));
				}
			}
		}

		return new ReferencedDialects(
			dialects.OrderBy(d => d.Name, StringComparer.Ordinal).ToEquatableArray(),
			diagnostics.ToEquatableArray());
	}

	private static ReferencedDialect? ReadManifest(
		AttributeData attribute,
		IAssemblySymbol assembly,
		INamedTypeSymbol enumAttribute,
		List<DiagnosticInfo> diagnostics)
	{
		var arguments = attribute.ConstructorArguments;
		var formatVersion = arguments.Length > 0 && arguments[0].Value is int version ? version : (int?)null;
		var name = arguments.Length > 1 ? arguments[1].Value as string : null;

		if (formatVersion != FormatVersion
			|| arguments.Length != 4
			|| name is null
			|| arguments[2].Value is not string @namespace
			|| arguments[3].Value is not ITypeSymbol dialectType)
		{
			diagnostics.Add(DiagnosticInfo.Create(
				DiagnosticDescriptors.UnsupportedManifest,
				null,
				assembly.Name,
				name ?? "?",
				formatVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?",
				FormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)));
			return null;
		}

		var includes = EquatableArray<string>.Empty;
		string? xmlHash = null;
		foreach (var named in attribute.NamedArguments)
		{
			switch (named.Key)
			{
				case "Includes" when named.Value.Kind == TypedConstantKind.Array:
					includes = named.Value.Values.Select(v => v.Value as string).OfType<string>().ToEquatableArray();
					break;
				case "XmlHash":
					xmlHash = named.Value.Value as string;
					break;
			}
		}

		return new ReferencedDialect(
			name,
			@namespace,
			dialectType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			includes,
			xmlHash,
			assembly.Name,
			ReadEnums(assembly, @namespace, enumAttribute));
	}

	private static EquatableArray<EnumFacts> ReadEnums(IAssemblySymbol assembly, string @namespace, INamedTypeSymbol enumAttribute)
	{
		var namespaceSymbol = FindNamespace(assembly.GlobalNamespace, @namespace);
		if (namespaceSymbol is null)
		{
			return EquatableArray<EnumFacts>.Empty;
		}

		var enums = new List<EnumFacts>();
		foreach (var type in namespaceSymbol.GetTypeMembers().OrderBy(t => t.Name, StringComparer.Ordinal))
		{
			if (type.TypeKind != TypeKind.Enum
				|| type.DeclaredAccessibility != Accessibility.Public
				|| type.EnumUnderlyingType is not { } underlyingType
				|| ToWireType(underlyingType.SpecialType) is not { } underlying)
			{
				continue;
			}

			var xmlName = type.GetAttributes()
				.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, enumAttribute))
				?.ConstructorArguments.FirstOrDefault().Value as string;
			if (xmlName is null)
			{
				continue;
			}

			var members = type.GetMembers()
				.OfType<IFieldSymbol>()
				.Where(f => f.HasConstantValue)
				.Select(f => new EnumMemberFacts(f.Name, ToUInt64(f.ConstantValue!)))
				.ToEquatableArray();

			var hasBitmask = namespaceSymbol.GetTypeMembers(type.Name + "Bitmask")
				.Any(t => t.DeclaredAccessibility == Accessibility.Public);

			enums.Add(new EnumFacts(xmlName, @namespace, type.Name, underlying, hasBitmask, members));
		}

		return enums.ToEquatableArray();
	}

	private static INamespaceSymbol? FindNamespace(INamespaceSymbol global, string name)
	{
		var current = global;
		foreach (var part in name.Split('.'))
		{
			current = current.GetNamespaceMembers().FirstOrDefault(n => n.Name == part);
			if (current is null)
			{
				return null;
			}
		}

		return current;
	}

	private static WireType? ToWireType(SpecialType type) => type switch
	{
		SpecialType.System_Byte => WireType.UInt8,
		SpecialType.System_SByte => WireType.Int8,
		SpecialType.System_UInt16 => WireType.UInt16,
		SpecialType.System_Int16 => WireType.Int16,
		SpecialType.System_UInt32 => WireType.UInt32,
		SpecialType.System_Int32 => WireType.Int32,
		SpecialType.System_UInt64 => WireType.UInt64,
		SpecialType.System_Int64 => WireType.Int64,
		_ => null,
	};

	private static ulong ToUInt64(object value) => value switch
	{
		byte v => v,
		sbyte v => unchecked((ulong)v),
		ushort v => v,
		short v => unchecked((ulong)v),
		uint v => v,
		int v => unchecked((ulong)v),
		ulong v => v,
		long v => unchecked((ulong)v),
		_ => 0,
	};
}
