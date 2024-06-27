using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Shmyndra.Mavlink.SourceGenerators;

internal class AssemblyResolver : IDisposable
{
	private readonly ConcurrentDictionary<string, Assembly> _forcedLoadedAssemblies = new ConcurrentDictionary<string, Assembly>();
	private readonly ResolveEventHandler _resolveEventHandler;
	private bool _isRegistered = false;

	public AssemblyResolver(params string[] assembliesToLoad)
	{
		_resolveEventHandler = new ResolveEventHandler(OnCurrentDomainAssemblyResolve);
		Register(assembliesToLoad);
	}

	private void Register(string[] assembliesToLoad)
	{
		if (!_isRegistered)
		{
			AppDomain.CurrentDomain.AssemblyResolve += _resolveEventHandler;

			foreach (var assemblyName in assembliesToLoad)
			{
				LoadAssembly(assemblyName);
			}

			_isRegistered = true;
		}
	}

	private void LoadAssembly(string assemblyName)
	{
		string assemblyPath = RuntimeEnvironment.GetRuntimeDirectory();
		string assemblyFullPath = Path.Combine(assemblyPath, $"{assemblyName}.dll");

		var loadedAssembly = Assembly.LoadFrom(assemblyFullPath);
		_forcedLoadedAssemblies.GetOrAdd(assemblyName, loadedAssembly);
	}

	private Assembly OnCurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
	{
		var name = new AssemblyName(args.Name).Name;
		_forcedLoadedAssemblies.TryGetValue(name, out Assembly outAssembly);
		return outAssembly;
	}

	public void Dispose()
	{
		if (_isRegistered)
		{
			AppDomain.CurrentDomain.AssemblyResolve -= _resolveEventHandler;
			_isRegistered = false;
		}
	}
}
