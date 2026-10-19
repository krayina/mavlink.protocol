using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mavlink;

/// <summary>
/// Exception factories for generated codec code: <c>throw CodecThrowHelper.X(...)</c>.
/// </summary>
/// <remarks>
/// Factories rather than <c>[DoesNotReturn]</c> throwers: the <c>throw</c> stays at the
/// call site, so C# reachability and definite assignment work without dummy returns,
/// while the message formatting — the bulk of the code — exists once in Core instead
/// of once per generated message. Every call site is a programming error by the caller;
/// nothing here is raised for data received from the wire.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CodecThrowHelper
{
	/// <summary>
	/// The caller supplied a buffer smaller than the payload being written.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ArgumentException DestinationTooShort(int required, int actual)
		=> new ArgumentException(
			$"Destination is {actual} bytes; at least {required} bytes are required.", "destination");

	/// <summary>
	/// A boxed message was routed to the companion of a different message type.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ArgumentException MessageTypeMismatch(Type expected, object? actual)
		=> new ArgumentException(
			$"Incorrect message type. Expected {expected.Name}, got {(actual is null ? "null" : actual.GetType().Name)}.",
			"message");

	/// <summary>
	/// Index outside a fixed-length array.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ArgumentOutOfRangeException IndexOutOfRange(int index, int length)
		=> new ArgumentOutOfRangeException("index", index, $"Index must be in [0, {length}).");

	/// <summary>
	/// A fixed-length array was built from a span of the wrong length.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static ArgumentException LengthMismatch(int required, int actual)
		=> new ArgumentException(
			$"Exactly {required} elements are required; got {actual}.", "values");
}
