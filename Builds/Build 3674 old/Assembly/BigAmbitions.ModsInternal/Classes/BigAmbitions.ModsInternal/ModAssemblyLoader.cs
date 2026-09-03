// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModAssemblyLoader
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BAModAPI;

internal static class ModAssemblyLoader
{
	private static readonly Dictionary<string, string> LoadedAssemblyPathsBySimpleName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> KnownModFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Assembly> LoadedAssembliesBySimpleName = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Assembly> LoadedAssembliesByPath = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> LoadedAssemblySimpleNamesByModId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static UnityModLogger _logger;

	internal static IReadOnlyCollection<string> ModFolders => KnownModFolders;

	internal static void Initialize(UnityModLogger logger)
	{
		_logger = logger;
	}

	internal static void AddKnownModFolder(string modFolder)
	{
		if (!string.IsNullOrWhiteSpace(modFolder))
		{
			KnownModFolders.Add(modFolder);
		}
	}

	internal static void RemoveKnownModFolder(string modFolder)
	{
		if (!string.IsNullOrWhiteSpace(modFolder))
		{
			KnownModFolders.Remove(modFolder);
		}
	}

	internal static void RemoveTrackedAssemblyForMod(string modId)
	{
		if (string.IsNullOrWhiteSpace(modId) || !LoadedAssemblySimpleNamesByModId.Remove(modId, out var value))
		{
			return;
		}
		bool flag = false;
		foreach (string value2 in LoadedAssemblySimpleNamesByModId.Values)
		{
			if (string.Equals(value2, value, StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			LoadedAssemblyPathsBySimpleName.Remove(value);
		}
	}

	internal static void Clear()
	{
		LoadedAssemblyPathsBySimpleName.Clear();
		KnownModFolders.Clear();
		LoadedAssembliesBySimpleName.Clear();
		LoadedAssembliesByPath.Clear();
		LoadedAssemblySimpleNamesByModId.Clear();
	}

	internal static Task<Assembly> LoadAssemblyFromPathAsync(string dllPath, string modId = null)
	{
		string text = NormalizePath(dllPath);
		if (string.IsNullOrWhiteSpace(text))
		{
			return Task.FromResult<Assembly>(null);
		}
		if (LoadedAssembliesByPath.TryGetValue(text, out var value))
		{
			RegisterLoadedAssembly(value, text, modId);
			return Task.FromResult(value);
		}
		string name = AssemblyName.GetAssemblyName(text).Name;
		if (!string.IsNullOrWhiteSpace(name) && TryGetLoadedAssembly(name, out var assembly))
		{
			RegisterLoadedAssembly(assembly, text, modId);
			return Task.FromResult(assembly);
		}
		Assembly assembly2 = LoadAssembly(text);
		RegisterLoadedAssembly(assembly2, text, modId);
		return Task.FromResult(assembly2);
	}

	internal static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		try
		{
			string name = new AssemblyName(args.Name).Name;
			if (string.IsNullOrWhiteSpace(name))
			{
				return null;
			}
			if (TryGetLoadedAssembly(name, out var assembly))
			{
				return assembly;
			}
			string requestingAssemblyDependencyPath = GetRequestingAssemblyDependencyPath(args.RequestingAssembly, name);
			if (!string.IsNullOrWhiteSpace(requestingAssemblyDependencyPath))
			{
				return LoadAssemblyFromPathAsync(requestingAssemblyDependencyPath).GetAwaiter().GetResult();
			}
			foreach (string knownModFolder in KnownModFolders)
			{
				requestingAssemblyDependencyPath = GetDependencyPath(knownModFolder, name);
				if (!string.IsNullOrWhiteSpace(requestingAssemblyDependencyPath))
				{
					return LoadAssemblyFromPathAsync(requestingAssemblyDependencyPath).GetAwaiter().GetResult();
				}
			}
		}
		catch (Exception exception)
		{
			_logger?.Error(exception);
			return null;
		}
		return null;
	}

	internal static async Task<bool> TryLoadDependenciesAsync(string modFolder, string modId, string modDisplayName, Action<string, string, string> registerFailedMod)
	{
		string path = Path.Combine(modFolder, "Dependencies");
		if (!Directory.Exists(path))
		{
			return true;
		}
		string[] files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		string[] array = files;
		foreach (string dependencyPath in array)
		{
			try
			{
				if (await LoadAssemblyFromPathAsync(dependencyPath) != null)
				{
					continue;
				}
				string arg = "Dependency '" + Path.GetFileName(dependencyPath) + "' could not be loaded.";
				registerFailedMod(modId, modDisplayName, arg);
				_logger?.Error("Failed to load dependency '" + dependencyPath + "'.");
				return false;
			}
			catch (Exception exception)
			{
				string arg2 = "Dependency '" + Path.GetFileName(dependencyPath) + "' failed to load.";
				registerFailedMod(modId, modDisplayName, arg2);
				_logger?.Error("Failed to load dependency '" + dependencyPath + "'.");
				_logger?.Error(exception);
				return false;
			}
		}
		return true;
	}

	private static bool TryGetLoadedAssembly(string assemblySimpleName, out Assembly assembly)
	{
		assembly = null;
		if (string.IsNullOrWhiteSpace(assemblySimpleName))
		{
			return false;
		}
		if (LoadedAssembliesBySimpleName.TryGetValue(assemblySimpleName, out assembly))
		{
			return true;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			AssemblyName name;
			try
			{
				name = assembly2.GetName();
			}
			catch
			{
				continue;
			}
			if (string.Equals(name.Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
			{
				RegisterLoadedAssembly(assembly2, GetAssemblyLocation(assembly2), null);
				assembly = assembly2;
				return true;
			}
		}
		return false;
	}

	private static void RegisterLoadedAssembly(Assembly assembly, string assemblyPath, string modId)
	{
		if (assembly == null)
		{
			return;
		}
		string name = assembly.GetName().Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			if (!string.IsNullOrWhiteSpace(assemblyPath))
			{
				_logger?.Warn("Loaded assembly path was registered without a valid assembly name: '" + assemblyPath + "'.");
			}
			return;
		}
		LoadedAssembliesBySimpleName[name] = assembly;
		string text = NormalizePath(assemblyPath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			LoadedAssembliesByPath[text] = assembly;
			LoadedAssemblyPathsBySimpleName[name] = text;
		}
		if (!string.IsNullOrWhiteSpace(modId))
		{
			LoadedAssemblySimpleNamesByModId[modId] = name;
		}
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static string GetRequestingAssemblyDependencyPath(Assembly requestingAssembly, string requestedName)
	{
		if (requestingAssembly == null)
		{
			return null;
		}
		string name = requestingAssembly.GetName().Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		if (!LoadedAssemblyPathsBySimpleName.TryGetValue(name, out var value))
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return GetDependencyPath(Path.GetDirectoryName(value), requestedName);
	}

	private static string GetDependencyPath(string folderPath, string requestedName)
	{
		if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
		{
			return null;
		}
		string text = Path.Combine(folderPath, requestedName + ".dll");
		if (!File.Exists(text))
		{
			return null;
		}
		return text;
	}

	private static string GetAssemblyLocation(Assembly assembly)
	{
		try
		{
			return assembly.Location;
		}
		catch
		{
			return null;
		}
	}

	private static Assembly LoadAssembly(string assemblyPath)
	{
		return Assembly.LoadFrom(assemblyPath);
	}
}
