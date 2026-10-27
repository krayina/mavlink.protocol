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

	internal static IMavlinkDialect ResolveDialect(IMavlinkDialect? dialect)
	{
		if (dialect != null)
		{
			return dialect;
		}

#if NET5_0_OR_GREATER
		return MavlinkDialects.Auto;
#else
		throw new ArgumentNullException(nameof(dialect),
			"An explicit dialect is required on netstandard targets " +
			"(automatic dialect discovery needs net5.0+). " +
			"Pass a single dialect, e.g. CommonDialect.Instance, or merge several with " +
			"MavlinkDialects.Combine(CommonDialect.Instance, MyCustomDialect.Instance) " +
			"— earlier dialects win on message-id overlap.");
#endif
	}
}
