using Microsoft.CodeAnalysis;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal static class ExceptionHandler
{
	public static void HandleException(SourceProductionContext context, Exception ex)
	{
		var rootException = GetRootException(ex);

		if (rootException.Message.Contains("System.ComponentModel.Annotations"))
		{
			// Generation already started
			// TODO: https://github.com/dotnet/runtime/discussions/102985
			return;
		}

		context.ReportDiagnostic(
			Diagnostic.Create(
				MavlinkGeneratorDiagnostics.GenericProtocolErrorRule,
				Location.None,
				ex.Message
			)
		);
	}

	private static Exception GetRootException(Exception ex)
	{
		var rootException = ex;
		while (rootException.InnerException is not null)
		{
			rootException = rootException.InnerException;
		}
		return rootException;
	}
}
