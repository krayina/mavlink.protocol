using Mavlink.Dialects;
using Mavlink.Transport;

namespace Mavlink;

public sealed partial class MavlinkChannel
{
	/// <summary>
	/// Creates a channel for <paramref name="dialect"/> and every dialect it includes;
	/// several dialects are merged with <see cref="MavlinkDialects.Combine"/>.
	/// </summary>
	public static MavlinkChannel Create(
		string connectionString,
		IMavlinkDialect dialect,
		Action<MavlinkChannelOptions>? configure = null)
	{
		return Create(MavlinkEndpoint.Parse(connectionString), dialect, configure);
	}

	/// <summary>
	/// Creates a channel for <paramref name="dialect"/> and every dialect it includes;
	/// several dialects are merged with <see cref="MavlinkDialects.Combine"/>.
	/// </summary>
	public static MavlinkChannel Create(
		MavlinkEndpoint endpoint,
		IMavlinkDialect dialect,
		Action<MavlinkChannelOptions>? configure = null)
	{
		if (endpoint is null)
		{
			throw new ArgumentNullException(nameof(endpoint));
		}

		if (dialect is null)
		{
			throw new ArgumentNullException(nameof(dialect));
		}

		var options = new MavlinkChannelOptions
		{
			PortProvider = endpoint.CreateProvider(),
			Dialect = dialect,
			ReconnectPolicy = endpoint.DefaultReconnectPolicy,
		};

		configure?.Invoke(options);
		return Create(options);
	}

	public static Task<MavlinkChannel> ConnectAsync(
		string connectionString,
		IMavlinkDialect dialect,
		CancellationToken ct = default)
	{
		return ConnectAsync(MavlinkEndpoint.Parse(connectionString), dialect, null, ct);
	}

	public static async Task<MavlinkChannel> ConnectAsync(
		MavlinkEndpoint endpoint,
		IMavlinkDialect dialect,
		Action<MavlinkChannelOptions>? configure = null,
		CancellationToken ct = default)
	{
		var channel = Create(endpoint, dialect, configure);

		try
		{
			await channel.ConnectAsync(ct).ConfigureAwait(false);
			return channel;
		}
		catch
		{
			await channel.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	/// <summary>
	/// Creates a channel for several dialects and everything they include; the same as passing
	/// <see cref="MavlinkDialects.Combine"/> of them.
	/// </summary>
	public static MavlinkChannel Create(string connectionString, params IMavlinkDialect[] dialects)
		=> Create(MavlinkEndpoint.Parse(connectionString), MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static MavlinkChannel Create(MavlinkEndpoint endpoint, params IMavlinkDialect[] dialects)
		=> Create(endpoint, MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static Task<MavlinkChannel> ConnectAsync(string connectionString, params IMavlinkDialect[] dialects)
		=> ConnectAsync(MavlinkEndpoint.Parse(connectionString), MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static Task<MavlinkChannel> ConnectAsync(MavlinkEndpoint endpoint, params IMavlinkDialect[] dialects)
		=> ConnectAsync(endpoint, MavlinkDialects.Combine(dialects));
}
