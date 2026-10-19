using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mavlink;

#if NET8_0_OR_GREATER
/// <summary>
/// Inline storage for a received payload (MAVLink maximum: 255 bytes).
/// </summary>
[InlineArray(MavlinkConstants.MAX_PAYLOAD_LENGTH)]
internal struct MavlinkPayloadBuffer
{
	private byte _element0;
}
#else
/// <summary>
/// Inline storage for a received payload (MAVLink maximum: 255 bytes).
/// </summary>
/// <remarks>
/// Pre-.NET 8 stand-in for an inline array, and exactly what the compiler emits for a
/// C# <c>fixed byte[255]</c> buffer — minus the need for <c>unsafe</c>. The declared size
/// reserves the bytes (struct copies and <c>default</c> cover all of them); they are
/// only ever reached through a managed <c>ref</c>, never a pointer, so nothing has to be
/// pinned and no reference can outlive the struct it points into.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = MavlinkConstants.MAX_PAYLOAD_LENGTH)]
internal struct MavlinkPayloadBuffer
{
	private byte _element0;
}
#endif

internal static class MavlinkPayloadBufferExtensions
{
	/// <summary>
	/// Reference to the first payload byte, valid for as long as <paramref name="buffer"/> is.
	/// Taken through <c>in</c> so a readonly field is referenced in place, never copied.
	/// The returned reference is writable only so the packet constructor can fill the buffer.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref byte FirstByte(this in MavlinkPayloadBuffer buffer)
		=> ref Unsafe.As<MavlinkPayloadBuffer, byte>(ref Unsafe.AsRef(in buffer));
}
