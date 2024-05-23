#if NETSTANDARD2_0_OR_GREATER
using System.ComponentModel;

namespace System.Runtime.CompilerServices;
[EditorBrowsable(EditorBrowsableState.Never)]
public record IsExternalInit;
#endif