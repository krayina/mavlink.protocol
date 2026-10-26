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

		foreach (var dialect in Flatten(roots))
		{
			foreach (var info in dialect.Messages)
			{
				// Earlier dialects win on overlap.
				if (!byId.TryGetValue(info.MessageId, out _))
				{
					byId.Add(info.MessageId, info);
				}

				if (!byType.TryGetValue(info.MessageType, out _))
				{
					byType.Add(info.MessageType, info);
				}
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
	/// Each dialect instance is returned once; cycles are tolerated.
	/// </summary>
	private static List<IMavlinkDialect> Flatten(IMavlinkDialect[] roots)
	{
		var result = new List<IMavlinkDialect>();
		var visited = new HashSet<IMavlinkDialect>(ReferenceComparer.Instance);

		foreach (var root in roots)
		{
			Visit(root ?? throw new ArgumentException("Dialects must not be null.", nameof(roots)), result, visited);
		}

		return result;

		static void Visit(IMavlinkDialect dialect, List<IMavlinkDialect> result, HashSet<IMavlinkDialect> visited)
		{
			if (!visited.Add(dialect))
			{
				return;
			}

			result.Add(dialect);

			foreach (var include in dialect.Includes)
			{
				Visit(include, result, visited);
			}
		}
	}

	private sealed class ReferenceComparer : IEqualityComparer<IMavlinkDialect>
	{
		public static readonly ReferenceComparer Instance = new();

		public bool Equals(IMavlinkDialect? x, IMavlinkDialect? y) => ReferenceEquals(x, y);

		public int GetHashCode(IMavlinkDialect obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
