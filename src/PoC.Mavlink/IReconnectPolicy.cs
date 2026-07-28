using System.IO.Ports;
using System.Net.Sockets;

namespace Mavlink;

public interface IReconnectPolicy
{
    bool RetryInitialConnect { get; }

    /// <summary>
    /// Decides whether and how long to wait before the next connection attempt.
    /// </summary>
    /// <param name="attempt">
    /// 1-based attempt number within the current reconnect series. Resets to 1
    /// each time a new series starts (initial connect, or recovery after a drop).
    /// Stateful policies may rely on <c>attempt == 1</c> as a series-start signal.
    /// </param>
    /// <param name="lastError">The exception that failed the previous attempt, if any.</param>
    /// <returns>Delay before the next attempt, or <c>null</c> to give up.</returns>
    TimeSpan? GetDelay(int attempt, Exception? lastError);
}

// Give up at once — e.g. a finished file replay.
public sealed class NoReconnectPolicy : IReconnectPolicy
{
    public static readonly NoReconnectPolicy Instance = new();

    private NoReconnectPolicy() { }

    public bool RetryInitialConnect => false;

    public TimeSpan? GetDelay(int attempt, Exception? lastError) => null;
}

// Fixed cadence with an optional ceiling of attempts.
public sealed class FixedReconnectPolicy : IReconnectPolicy
{
    private readonly TimeSpan _delay;
    private readonly int? _maxAttempts;

    public FixedReconnectPolicy(TimeSpan delay, int? maxAttempts = null, bool retryInitialConnect = false)
    {
        _delay = delay;
        _maxAttempts = maxAttempts;
        RetryInitialConnect = retryInitialConnect;
    }

    public bool RetryInitialConnect { get; }

    public TimeSpan? GetDelay(int attempt, Exception? lastError)
        => _maxAttempts is { } max && attempt > max ? null : _delay;
}

// Classic backoff: 0.5s → 1s → 2s → 4s … capped at 'max'.
public sealed class ExponentialBackoffPolicy : IReconnectPolicy
{
    private readonly TimeSpan _initial, _max;
    private readonly double _factor;

    public ExponentialBackoffPolicy(
        TimeSpan? initial = null,
        TimeSpan? max = null,
        double factor = 2.0,
        bool retryInitialConnect = false)
    {
        _initial = initial ?? TimeSpan.FromMilliseconds(500);
        _max = max ?? TimeSpan.FromSeconds(30);
        _factor = factor;
        RetryInitialConnect = retryInitialConnect;
    }

    public bool RetryInitialConnect { get; }

    public TimeSpan? GetDelay(int attempt, Exception? lastError)
    {
        var ticks = (long)Math.Min(
            _initial.Ticks * Math.Pow(_factor, attempt - 1), _max.Ticks);
        return TimeSpan.FromTicks(ticks);
    }
}

internal sealed class TcpReconnectPolicy : IReconnectPolicy
{
    private readonly TimeSpan _refusedInitial = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _max = TimeSpan.FromSeconds(15);

    public bool RetryInitialConnect => true;

    public TimeSpan? GetDelay(int attempt, Exception? lastError)
    {
        var sockEx = FindSocketException(lastError);

        if (sockEx?.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset)
        {
            var ticks = (long)Math.Min(_refusedInitial.Ticks * Math.Pow(2, attempt - 1), _max.Ticks);
            return TimeSpan.FromTicks(ticks);
        }

        if (lastError is MavlinkConnectionException { InnerException: null } mce
            && mce.Message.Contains("timed out"))
        {
            return TimeSpan.FromMilliseconds(250);
        }

        var t = (long)Math.Min(TimeSpan.FromSeconds(1).Ticks * Math.Pow(2, attempt - 1), _max.Ticks);
        return TimeSpan.FromTicks(t);
    }

    private static SocketException? FindSocketException(Exception? ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SocketException se)
            {
                return se;
            }
        }
        return null;
    }
}

internal sealed class SerialReconnectPolicy : IReconnectPolicy
{
    private readonly string _portName;
    private readonly TimeSpan _pollWhenAbsent = TimeSpan.FromMilliseconds(500);
    private readonly TimeSpan _retryWhenPresent = TimeSpan.FromMilliseconds(200);

    public SerialReconnectPolicy(string portName) => _portName = portName;

    public bool RetryInitialConnect => true;

    public TimeSpan? GetDelay(int attempt, Exception? lastError)
    {
        bool present;
        try
        {
            present = Array.Exists(SerialPort.GetPortNames(),
                n => string.Equals(n, _portName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            present = true;
        }

        if (!present)
        {
            return _pollWhenAbsent;
        }
        return _retryWhenPresent;
    }
}
