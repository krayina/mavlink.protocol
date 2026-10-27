namespace Mavlink.Dialects;

public static class MavlinkDialects
{
	/// <summary>
	/// Merges several dialects, together with everything they include, into one.
	/// A dialect reachable through several includes is taken once. A message id or
	/// type defined by two different dialects is a conflict and throws.
	/// </summary>
	/// <exception cref="ArgumentException">
	/// No dialects were given, a message id or type is defined twice, or two different
	/// dialect instances share a name.
	/// </exception>
	public static IMavlinkDialect Combine(params IMavlinkDialect[] dialects)
	{
		if (dialects is null || dialects.Length == 0)
		{
			throw new ArgumentException("At least one dialect is required.", nameof(dialects));
		}

		// A single dialect without includes already resolves everything by itself.
		return dialects.Length == 1 && dialects[0] is { Includes.Count: 0 } single
			? single
			: new MavlinkDialectCompositor(dialects);
	}
}
