// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModCompatibilityValidator
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BAModAPI;

internal static class ModCompatibilityValidator
{
	private static readonly string[] GameAssemblyNamePrefixes = new string[2] { "BAModAPI", "BigAmbitions" };

	internal static bool TryValidateGameAssemblyCompatibility(Assembly modAssembly, string modFolder, string modId, string modDisplayName, IReadOnlyCollection<string> knownModFolders, Action<string, string, string> registerFailedMod, UnityModLogger logger)
	{
		AssemblyName[] referencedAssemblies;
		try
		{
			referencedAssemblies = modAssembly.GetReferencedAssemblies();
		}
		catch (Exception exception)
		{
			registerFailedMod(modId, modDisplayName, "The mod assembly references could not be read for compatibility validation.");
			logger.Error("Failed to read assembly references from '" + modAssembly.FullName + "'.");
			logger.Error(exception);
			return false;
		}
		List<string> list = new List<string>();
		AssemblyName[] array = referencedAssemblies;
		foreach (AssemblyName assemblyName in array)
		{
			if (!string.IsNullOrWhiteSpace(assemblyName.Name) && !(assemblyName.Version == null) && TryGetLoadedGameAssembly(assemblyName.Name, modFolder, knownModFolders, out var assembly))
			{
				Version version = assembly.GetName().Version;
				if (!(version == null) && assemblyName.Version.Major != version.Major)
				{
					list.Add($"{assemblyName.Name} requires major {assemblyName.Version.Major} but game is " + $"{version.Major}");
				}
			}
		}
		if (list.Count == 0)
		{
			return true;
		}
		string text = string.Join("; ", list);
		registerFailedMod(modId, modDisplayName, "Incompatible game library major versions. " + text + ".");
		logger.Warn("Skipped mod '" + modDisplayName + "' because of assembly incompatibilities: " + text + ".");
		return false;
	}

	internal static bool TryValidateModAssemblyCompatibility(Assembly modAssembly, string modId, string modDisplayName, IReadOnlyDictionary<string, string> modIdByAssemblySimpleName, IReadOnlyDictionary<string, Version> modAssemblyVersionBySimpleName, ISet<string> loadedModIds, ISet<string> failedModIds, Action<string, string, string> registerFailedMod, UnityModLogger logger, out string deferredDependencyModId)
	{
		deferredDependencyModId = null;
		AssemblyName[] referencedAssemblies;
		try
		{
			referencedAssemblies = modAssembly.GetReferencedAssemblies();
		}
		catch (Exception exception)
		{
			registerFailedMod(modId, modDisplayName, "The mod assembly references could not be read for dependency validation.");
			logger.Error("Failed to read assembly references from '" + modAssembly.FullName + "'.");
			logger.Error(exception);
			return false;
		}
		List<string> list = new List<string>();
		AssemblyName[] array = referencedAssemblies;
		foreach (AssemblyName assemblyName in array)
		{
			if (!string.IsNullOrWhiteSpace(assemblyName.Name) && modIdByAssemblySimpleName.TryGetValue(assemblyName.Name, out var value) && !string.Equals(value, modId, StringComparison.OrdinalIgnoreCase))
			{
				if (failedModIds.Contains(value))
				{
					registerFailedMod(modId, modDisplayName, "Dependency mod '" + value + "' failed to load.");
					return false;
				}
				if (!loadedModIds.Contains(value))
				{
					deferredDependencyModId = value;
					return true;
				}
				if (!(assemblyName.Version == null) && modAssemblyVersionBySimpleName.TryGetValue(assemblyName.Name, out var value2) && !(value2 == null) && assemblyName.Version.Major != value2.Major)
				{
					list.Add($"{assemblyName.Name} requires major {assemblyName.Version.Major} but mod provides " + $"{value2.Major}");
				}
			}
		}
		if (list.Count == 0)
		{
			return true;
		}
		string text = string.Join("; ", list);
		registerFailedMod(modId, modDisplayName, "Incompatible mod dependency major versions. " + text + ".");
		logger.Warn("Skipped mod '" + modDisplayName + "' because of mod dependency incompatibilities: " + text + ".");
		return false;
	}

	private static bool TryGetLoadedGameAssembly(string assemblySimpleName, string modFolder, IReadOnlyCollection<string> knownModFolders, out Assembly assembly)
	{
		assembly = null;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			if (!(assembly2 == null))
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
				if (string.Equals(name?.Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase) && IsGameAssembly(assembly2, modFolder, knownModFolders))
				{
					assembly = assembly2;
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsGameAssembly(Assembly assembly, string modFolder, IReadOnlyCollection<string> knownModFolders)
	{
		string name = assembly.GetName().Name;
		if (string.IsNullOrWhiteSpace(name) || !IsGameAssemblyName(name))
		{
			return false;
		}
		string assemblyLocation = GetAssemblyLocation(assembly);
		if (string.IsNullOrWhiteSpace(assemblyLocation))
		{
			return true;
		}
		if (IsPathInsideFolder(assemblyLocation, modFolder))
		{
			return false;
		}
		foreach (string knownModFolder in knownModFolders)
		{
			if (IsPathInsideFolder(assemblyLocation, knownModFolder))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsGameAssemblyName(string assemblyName)
	{
		string[] gameAssemblyNamePrefixes = GameAssemblyNamePrefixes;
		foreach (string value in gameAssemblyNamePrefixes)
		{
			if (assemblyName.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
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

	private static bool IsPathInsideFolder(string path, string folderPath)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(folderPath))
		{
			return false;
		}
		string text = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string text2 = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		string value = text2 + directorySeparatorChar;
		return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}
}
