using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Mavlink.Dialects;

/// <summary>
/// Resolves messages of several dialects and of everything they include.
/// The include trees are flattened once in the constructor (each dialect is taken
/// once, however many dialects include it), and lookups go through tables built
/// from <see cref="IMavlinkDialect.Messages"/>.
/// </summary>
/// <remarks>
/// After flattening, every message belongs to exactly one dialect, so an id or a type
/// claimed by two different dialects is a real conflict (e.g. two vendor dialects that
/// picked the same id) and is rejected instead of being resolved by order.
/// </remarks>
internal sealed class MavlinkDialectCompositor : IMavlinkDialect
{
	private readonly IMavlinkDialect[] _roots;

#if NET8_0_OR_GREATER
	private readonly FrozenDictionary<uint, IMavlinkMessageInfo> _byId;
	private readonly FrozenDictionary<Type, IMavlinkMessageInfo> _byType;
#else
	private readonly Dictionary<uint, IMavlinkMessageInfo> _byId;
	private readonly Dictionary<Type, IMavlinkMessageInfo> _byType;
#endif

	public MavlinkDialectCompositor(IMavlinkDialect[] roots)
	{
		_roots = roots ?? throw new ArgumentNullException(nameof(roots));

		var byId = new Dictionary<uint, IMavlinkMessageInfo>();
		var byType = new Dictionary<Type, IMavlinkMessageInfo>();
		var ownerById = new Dictionary<uint, IMavlinkDialect>();
		var ownerByType = new Dictionary<Type, IMavlinkDialect>();

		foreach (var dialect in Flatten(roots))
		{
			foreach (var info in dialect.Messages)
			{
				if (ownerById.TryGetValue(info.MessageId, out var idOwner))
				{
					throw ConflictingMessageId(info.MessageId, idOwner, dialect);
				}

				if (ownerByType.TryGetValue(info.MessageType, out var typeOwner))
				{
					throw ConflictingMessageType(info.MessageType, typeOwner, dialect);
				}

				ownerById.Add(info.MessageId, dialect);
				ownerByType.Add(info.MessageType, dialect);
				byId.Add(info.MessageId, info);
				byType.Add(info.MessageType, info);
			}
		}

#if NET8_0_OR_GREATER
		_byId = byId.ToFrozenDictionary();
		_byType = byType.ToFrozenDictionary();
#else
		_byId = byId;
		_byType = byType;
#endif
	}

	public string Name => "DialectCompositor";

	public IReadOnlyList<IMavlinkDialect> Includes => _roots;

	public IReadOnlyList<IMavlinkMessageInfo> Messages => Array.Empty<IMavlinkMessageInfo>();

	public IMavlinkMessageInfo? GetInfo(Type type)
		=> _byType.TryGetValue(type, out var info) ? info : null;

	public IMavlinkMessageInfo? GetInfo(uint messageId)
		=> _byId.TryGetValue(messageId, out var info) ? info : null;

	/// <summary>
	/// Depth-first, pre-order: a dialect comes before the dialects it includes.
	/// Each dialect instance is returned once; cycles are tolerated. Two different
	/// instances with the same <see cref="IMavlinkDialect.Name"/> are rejected: that is
	/// one dialect XML generated into two assemblies.
	/// </summary>
	private static List<IMavlinkDialect> Flatten(IMavlinkDialect[] roots)
	{
		var result = new List<IMavlinkDialect>();
		var visited = new HashSet<IMavlinkDialect>(ReferenceComparer.Instance);
		var byName = new Dictionary<string, IMavlinkDialect>(StringComparer.Ordinal);

		foreach (var root in roots)
		{
			Visit(root ?? throw new ArgumentException("Dialects must not be null.", nameof(roots)), result, visited, byName);
		}

		return result;

		static void Visit(
			IMavlinkDialect dialect,
			List<IMavlinkDialect> result,
			HashSet<IMavlinkDialect> visited,
			Dictionary<string, IMavlinkDialect> byName)
		{
			if (!visited.Add(dialect))
			{
				return;
			}

			// A nested compositor is transparent: only its roots matter.
			if (dialect is MavlinkDialectCompositor nested)
			{
				foreach (var root in nested._roots)
				{
					Visit(root, result, visited, byName);
				}

				return;
			}

			if (byName.TryGetValue(dialect.Name, out var sameName))
			{
				throw DuplicateDialectName(sameName, dialect);
			}

			byName.Add(dialect.Name, dialect);
			result.Add(dialect);

			foreach (var include in dialect.Includes)
			{
				Visit(include, result, visited, byName);
			}
		}
	}

	private static ArgumentException ConflictingMessageId(uint messageId, IMavlinkDialect first, IMavlinkDialect second)
		=> new ArgumentException(
			$"Message id {messageId} is defined by both dialect '{first.Name}' and dialect '{second.Name}'.",
			"dialects");

	private static ArgumentException ConflictingMessageType(Type messageType, IMavlinkDialect first, IMavlinkDialect second)
		=> new ArgumentException(
			$"Message type {messageType.FullName} is registered by both dialect '{first.Name}' and dialect '{second.Name}'.",
			"dialects");

	private static ArgumentException DuplicateDialectName(IMavlinkDialect first, IMavlinkDialect second)
		=> new ArgumentException(
			$"Two different dialect instances are named '{first.Name}' " +
			$"({first.GetType().AssemblyQualifiedName} and {second.GetType().AssemblyQualifiedName}). " +
			"The same dialect XML is probably generated into two assemblies.",
			"dialects");

	private sealed class ReferenceComparer : IEqualityComparer<IMavlinkDialect>
	{
		public static readonly ReferenceComparer Instance = new();

		public bool Equals(IMavlinkDialect? x, IMavlinkDialect? y) => ReferenceEquals(x, y);

		public int GetHashCode(IMavlinkDialect obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
