using Mavlink.Dialects;
using Mavlink.Transport;

namespace Mavlink;

public sealed partial class MavlinkClient
{
	/// <summary>
	/// Creates a client that OWNS its channel: disposing the client also
	/// disposes the channel (which the caller never sees, so nobody else
	/// could dispose it). For a shared channel with multiple client
	/// identities, use <see cref="MavlinkChannel.CreateClient"/> instead —
	/// those clients do NOT dispose the shared channel.
	/// The channel resolves <paramref name="dialect"/> and every dialect it includes;
	/// several dialects are merged with <see cref="MavlinkDialects.Combine"/>.
	/// </summary>
	public static MavlinkClient Create(
		string connectionString,
		IMavlinkDialect dialect,
		byte systemId = 255,
		byte componentId = 190,
		Action<MavlinkChannelOptions>? configure = null)
	{
		return Create(MavlinkEndpoint.Parse(connectionString), dialect, systemId, componentId, configure);
	}

	/// <inheritdoc cref="Create(string, IMavlinkDialect, byte, byte, Action{MavlinkChannelOptions}?)"/>
	public static MavlinkClient Create(
		MavlinkEndpoint endpoint,
		IMavlinkDialect dialect,
		byte systemId = 255,
		byte componentId = 190,
		Action<MavlinkChannelOptions>? configure = null)
	{
		var channel = MavlinkChannel.Create(endpoint, dialect, configure);

		return new MavlinkClient(
			channel, systemId, componentId,
			defaultSendVersion: null, ownsChannel: true);
	}

	/// <inheritdoc cref="Create(string, IMavlinkDialect, byte, byte, Action{MavlinkChannelOptions}?)"/>
	public static Task<MavlinkClient> ConnectAsync(
		string connectionString,
		IMavlinkDialect dialect,
		byte systemId = 255,
		byte componentId = 190,
		CancellationToken ct = default)
	{
		return ConnectAsync(MavlinkEndpoint.Parse(connectionString), dialect, systemId, componentId, null, ct);
	}

	/// <inheritdoc cref="Create(string, IMavlinkDialect, byte, byte, Action{MavlinkChannelOptions}?)"/>
	public static async Task<MavlinkClient> ConnectAsync(
		MavlinkEndpoint endpoint,
		IMavlinkDialect dialect,
		byte systemId = 255,
		byte componentId = 190,
		Action<MavlinkChannelOptions>? configure = null,
		CancellationToken ct = default)
	{
		var client = Create(endpoint, dialect, systemId, componentId, configure);

		try
		{
			await client.ConnectAsync(ct).ConfigureAwait(false);
			return client;
		}
		catch
		{
			await client.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	/// <summary>
	/// Creates a client that owns its channel, for several dialects and everything they include,
	/// with the default identity (system 255, component 190); the same as passing
	/// <see cref="MavlinkDialects.Combine"/> of them.
	/// </summary>
	public static MavlinkClient Create(string connectionString, params IMavlinkDialect[] dialects)
		=> Create(connectionString, MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static MavlinkClient Create(MavlinkEndpoint endpoint, params IMavlinkDialect[] dialects)
		=> Create(endpoint, MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static Task<MavlinkClient> ConnectAsync(string connectionString, params IMavlinkDialect[] dialects)
		=> ConnectAsync(connectionString, MavlinkDialects.Combine(dialects));

	/// <inheritdoc cref="Create(string, IMavlinkDialect[])"/>
	public static Task<MavlinkClient> ConnectAsync(MavlinkEndpoint endpoint, params IMavlinkDialect[] dialects)
		=> ConnectAsync(endpoint, MavlinkDialects.Combine(dialects));
}
