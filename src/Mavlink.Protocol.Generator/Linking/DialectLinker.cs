using System.Collections.Immutable;
using Mavlink.Protocol.Generator.Common;
using Mavlink.Protocol.Generator.Diagnostics;
using Mavlink.Protocol.Generator.Parsing;

namespace Mavlink.Protocol.Generator.Linking;

/// <summary>
/// Builds the include graph of the project's own dialect files. Every include must be an own
/// file or a dialect from a referenced assembly (MAVGEN002); cycles are errors (MAVGEN003);
/// two own files with the same name are errors (MAVGEN014).
/// </summary>
internal static class DialectLinker
{
	public static DialectGraph Link(
		ImmutableArray<ParsedFile> files,
		ImmutableArray<ReferencedDialect> referenced,
		CancellationToken cancellationToken)
	{
		var diagnostics = new List<DiagnosticInfo>();

		// Sorted so the result and the reported duplicates do not depend on item order.
		var own = new Dictionary<string, (string Name, ParsedFile File)>(DialectName.Comparer);
		foreach (var file in files.OrderBy(f => f.Path, StringComparer.Ordinal))
		{
			var name = DialectName.FromPath(file.Path);
			if (own.TryGetValue(name, out var first))
			{
				diagnostics.Add(DiagnosticInfo.Create(
					DiagnosticDescriptors.DuplicateDialectFile,
					new LocationInfo(file.Path, default, default),
					first.Name,
					first.File.Path));
				continue;
			}

			own.Add(name, (name, file));
		}

		// Duplicate manifests among references are reported when references are read.
		var references = new Dictionary<string, ReferencedDialect>(DialectName.Comparer);
		foreach (var dialect in referenced)
		{
			if (!references.ContainsKey(dialect.Name))
			{
				references.Add(dialect.Name, dialect);
			}
		}

		var ownNames = own.Keys.OrderBy(n => n, StringComparer.Ordinal).ToList();

		var edges = new Dictionary<string, List<Edge>>(DialectName.Comparer);
		foreach (var name in ownNames)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var file = own[name].File;
			var list = new List<Edge>();
			foreach (var include in file.Includes)
			{
				var target = DialectName.FromPath(include.Path);
				DialectReference reference;
				if (own.TryGetValue(target, out var ownTarget))
				{
					reference = new DialectReference(ownTarget.Name, DialectOrigin.Own);
				}
				else if (references.TryGetValue(target, out var referencedTarget))
				{
					reference = new DialectReference(referencedTarget.Name, DialectOrigin.Referenced);
				}
				else
				{
					diagnostics.Add(DiagnosticInfo.Create(
						DiagnosticDescriptors.UnresolvedInclude,
						include.Location,
						FileName(file.Path),
						include.Path));
					continue;
				}

				if (!list.Any(e => DialectName.Comparer.Equals(e.Target.Name, reference.Name)))
				{
					list.Add(new Edge(reference, include));
				}
			}

			edges.Add(name, list);
		}

		var order = SortAndBreakCycles(ownNames, own, edges, diagnostics);

		var dialects = order
			.Select(name => new LinkedDialect(
				name,
				own[name].File,
				edges[name].Select(e => e.Target).ToEquatableArray(),
				Closure(name, edges, references)))
			.ToEquatableArray();

		return new DialectGraph(dialects, diagnostics.ToEquatableArray());
	}

	// Depth-first: a dialect goes to the result after everything it includes. An include that
	// leads back to a dialect still being visited closes a cycle; it is reported and removed.
	private static List<string> SortAndBreakCycles(
		List<string> ownNames,
		Dictionary<string, (string Name, ParsedFile File)> own,
		Dictionary<string, List<Edge>> edges,
		List<DiagnosticInfo> diagnostics)
	{
		var order = new List<string>();
		var state = new Dictionary<string, VisitState>(DialectName.Comparer);
		var path = new List<string>();

		void Visit(string name)
		{
			state[name] = VisitState.InProgress;
			path.Add(name);

			var list = edges[name];
			for (var i = 0; i < list.Count; i++)
			{
				var target = list[i].Target;
				if (target.Origin != DialectOrigin.Own)
				{
					continue;
				}

				state.TryGetValue(target.Name, out var targetState);
				if (targetState == VisitState.InProgress)
				{
					var cycle = path
						.Skip(path.FindIndex(n => DialectName.Comparer.Equals(n, target.Name)))
						.Append(target.Name)
						.Select(n => FileName(own[n].File.Path));
					diagnostics.Add(DiagnosticInfo.Create(
						DiagnosticDescriptors.IncludeCycle,
						list[i].Include.Location,
						string.Join(" -> ", cycle)));
					list.RemoveAt(i);
					i--;
				}
				else if (targetState == VisitState.NotVisited)
				{
					Visit(target.Name);
				}
			}

			path.RemoveAt(path.Count - 1);
			state[name] = VisitState.Done;
			order.Add(name);
		}

		foreach (var name in ownNames)
		{
			if (!state.ContainsKey(name))
			{
				Visit(name);
			}
		}

		return order;
	}

	// Breadth-first, so the nearest dialect comes first (it wins when a derived enum is looked up).
	private static EquatableArray<DialectReference> Closure(
		string name,
		Dictionary<string, List<Edge>> edges,
		Dictionary<string, ReferencedDialect> references)
	{
		var result = new List<DialectReference>();
		var seen = new HashSet<string>(DialectName.Comparer) { name };
		var queue = new Queue<DialectReference>();

		void Enqueue(DialectReference reference)
		{
			if (seen.Add(reference.Name))
			{
				result.Add(reference);
				queue.Enqueue(reference);
			}
		}

		foreach (var edge in edges[name])
		{
			Enqueue(edge.Target);
		}

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (current.Origin == DialectOrigin.Own)
			{
				foreach (var edge in edges[current.Name])
				{
					Enqueue(edge.Target);
				}

				continue;
			}

			// A compiled dialect includes only compiled dialects; one missing from the
			// references cannot be followed and is reported when references are read.
			foreach (var include in references[current.Name].Includes)
			{
				if (references.TryGetValue(include, out var target))
				{
					Enqueue(new DialectReference(target.Name, DialectOrigin.Referenced));
				}
			}
		}

		return result.ToEquatableArray();
	}

	private static string FileName(string path)
	{
		var start = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\')) + 1;
		return path.Substring(start);
	}

	private readonly record struct Edge(DialectReference Target, ParsedInclude Include);

	private enum VisitState
	{
		NotVisited,
		InProgress,
		Done,
	}
}
