using System.Net.Sockets;

namespace Shmyndra.Mavlink.Communication.Tcp;

/// <summary>
/// Provides extension methods for working with asynchronous socket operations using the SocketAwaitable pattern.
/// </summary>
internal static class SocketExtensions
{
	/// <summary>
	/// Initiates an asynchronous receive operation on the provided socket using a SocketAwaitable object.
	/// </summary>
	/// <param name="socket">The socket on which to perform the receive operation.</param>
	/// <param name="awaitable">The SocketAwaitable object that manages the asynchronous operation.</param>
	/// <returns>The same SocketAwaitable object, configured for awaiting the result of the receive operation.</returns>
	public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
	{
		// Reset the awaitable object for reuse in a new asynchronous operation.
		awaitable.Reset();

		// Start the asynchronous receive operation. If it completes synchronously, 
		// immediately schedule the continuation.
		if (!socket.ReceiveAsync(awaitable.EventArgs))
		{
			// Ensure the continuation is called if the operation completes synchronously.
			awaitable.GetAwaiter().OnCompleted(() => { });
		}

		return awaitable;
	}
}
