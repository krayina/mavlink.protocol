using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shmyndra.Mavlink.Communication.Tcp;

/// <summary>
/// Provides an awaitable wrapper for asynchronous socket operations using SocketAsyncEventArgs.
/// </summary>
internal sealed class SocketAwaitable : INotifyCompletion
{
	/// <summary>
	/// A sentinel action used to mark the completion of the asynchronous operation.
	/// </summary>
	private readonly static Action Sentinel = () => { };

	/// <summary>
	/// Indicates whether the asynchronous operation has been completed.
	/// </summary>
	internal bool WasCompleted;

	/// <summary>
	/// Stores the continuation action to be invoked when the operation completes.
	/// </summary>
	internal Action? Continuation;

	/// <summary>
	/// The SocketAsyncEventArgs instance used for the socket operation.
	/// </summary>
	internal SocketAsyncEventArgs EventArgs;

	/// <summary>
	/// Initializes a new instance of the SocketAwaitable class, subscribing to the Completed event of the provided SocketAsyncEventArgs.
	/// </summary>
	/// <param name="eventArgs">The SocketAsyncEventArgs to be used for the operation.</param>
	public SocketAwaitable(SocketAsyncEventArgs eventArgs)
	{
		if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
		EventArgs = eventArgs;

		// Subscribe to the Completed event of the SocketAsyncEventArgs.
		eventArgs.Completed += delegate
		{
			// Set the continuation to the sentinel if not already set.
			var previously = Continuation ?? Interlocked.CompareExchange(ref Continuation, Sentinel, null);

			// If a continuation was previously set, invoke it.
			if (previously != null)
			{
				previously();
			}
		};
	}

	/// <summary>
	/// Resets the internal state to allow the object to be reused.
	/// </summary>
	internal void Reset()
	{
		WasCompleted = false;
		Continuation = null;
	}

	/// <summary>
	/// Returns the current instance as the awaiter for the async operation.
	/// </summary>
	/// <returns>The current instance.</returns>
	public SocketAwaitable GetAwaiter() => this;

	/// <summary>
	/// Gets a value indicating whether the asynchronous operation has been completed.
	/// </summary>
	public bool IsCompleted => WasCompleted;

	/// <summary>
	/// Schedules the continuation action to be invoked upon the completion of the asynchronous operation.
	/// </summary>
	/// <param name="continuation">The continuation action to be invoked.</param>
	public void OnCompleted(Action continuation)
	{
		// Attempt to set the continuation; if already completed, run it immediately.
		if (Continuation == Sentinel || Interlocked.CompareExchange(ref Continuation, continuation, null) == Sentinel)
		{
			Task.Run(continuation);
		}
	}

	/// <summary>
	/// Gets the result of the asynchronous operation and throws a SocketException if the operation failed.
	/// </summary>
	/// <exception cref="SocketException">Thrown if the socket operation resulted in an error.</exception>
	public void GetResult()
	{
		if (EventArgs.SocketError != SocketError.Success)
		{
			throw new SocketException((int)EventArgs.SocketError);
		}
	}
}
