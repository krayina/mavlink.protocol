using Microsoft.CodeAnalysis;
using Mavlink.Protocol.Generator.Common;

namespace Mavlink.Protocol.Generator.Diagnostics;

/// <summary>
/// An equatable stand-in for <see cref="Diagnostic"/>: stages collect these in their models
/// and the generator reports them at the end of the pipeline.
/// </summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> MessageArgs)
{
	public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] messageArgs)
		=> new(descriptor, location, messageArgs.ToEquatableArray());

	public Diagnostic ToDiagnostic()
		=> Diagnostic.Create(
			Descriptor,
			Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None,
			MessageArgs.Cast<object?>().ToArray());
}
