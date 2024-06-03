using System.Reflection;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Shmyndra.Mavlink.Tasks;

public class RebuildGenerator : Task, ICancelableTask
{
	private CancellationTokenSource? _cancellationTokenSource;

	public override bool Execute()
	{
		_cancellationTokenSource = new CancellationTokenSource();
		var token = _cancellationTokenSource.Token;

		System.Threading.Tasks.Task.Run(() => ExecuteTask(token), token).Wait();
		return !token.IsCancellationRequested;
	}

	private void ExecuteTask(CancellationToken token)
	{
		//if (!Debugger.IsAttached)
		//{
		//	Debugger.Launch();
		//}

		try
		{
			if (token.IsCancellationRequested)
			{
				return;
			}

			FunctionsAssemblyResolver.RedirectAssembly();

			Log.LogMessage(MessageImportance.High, "Generator cache cleared.");
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);
		}
	}

	public void Cancel()
	{
		_cancellationTokenSource?.Cancel();
	}
}

public class FunctionsAssemblyResolver
{
	public static void RedirectAssembly()
	{
		var list = AppDomain.CurrentDomain.GetAssemblies().OrderByDescending(a => a.FullName).Select(a => a.FullName).ToList();
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
	}

	private static Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
	{
		var requestedAssembly = new AssemblyName(args.Name);
		Assembly? assembly = null;
		AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
		try
		{
			assembly = Assembly.Load(requestedAssembly.Name);
		}
		catch (Exception ex)
		{
			var test = ex;
		}
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		return assembly;
	}

}
