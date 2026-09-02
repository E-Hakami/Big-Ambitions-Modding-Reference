// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModDiscoveryRegistry
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BAModAPI;
using BAModAPI.Services;
using BigAmbitions;
using BigAmbitions.ModsInternal;
using Localizor;
using UnityEngine;

public static class ModDiscoveryRegistry
{
	private static readonly (ModActivationScope Scope, Type AttributeType)[] ScopeAttributeMappings = new(ModActivationScope, Type)[5]
	{
		(ModActivationScope.Initialization, typeof(ModEntryOnInitializationLoadAttribute)),
		(ModActivationScope.MainMenu, typeof(ModEntryMainMenuAttribute)),
		(ModActivationScope.City, typeof(ModEntryOnCityLoadAttribute)),
		(ModActivationScope.BlueprintCreator, typeof(ModEntryOnBlueprintCreatorLoadAttribute)),
		(ModActivationScope.Intro, typeof(ModEntryOnIntroLoadAttribute))
	};

	private static readonly Dictionary<string, string> FailedModReasonsById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<ModActivationScope, List<DiscoveredModEntry>> EntriesByScope = new Dictionary<ModActivationScope, List<DiscoveredModEntry>>();

	private static readonly SemaphoreSlim DiscoverySemaphore = new SemaphoreSlim(1, 1);

	private static bool HasPendingFileChanges;

	private static bool IsRefreshingOnFocus;

	private static string SteamWorkshopModsPath;

	private static FileSystemWatcher ModsLocalWatcher;

	private static FileSystemWatcher SteamWorkshopWatcher;

	private static UnityModLogger Logger;

	private static bool Initialized;

	public static string ModsLocalPath => Path.Combine(Application.persistentDataPath, "ModsLocal");

	public static IReadOnlyDictionary<ModActivationScope, List<DiscoveredModEntry>> Entries => EntriesByScope;

	public static IReadOnlyDictionary<string, string> FailedModReasons => FailedModReasonsById;

	public static bool HasDiscoveredEntries => EntriesByScope.Values.Any((List<DiscoveredModEntry> entries) => entries.Count > 0);

	public static event Action OnDiscoveryUpdated;

	private static void RegisterFailedMod(string modId, string modDisplayName, string reason)
	{
		if (!string.IsNullOrWhiteSpace(modId))
		{
			FailedModReasonsById[modId] = modDisplayName + ": " + reason;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticState()
	{
		Application.quitting -= OnApplicationQuitting;
		Application.focusChanged -= OnApplicationFocusChanged;
		AppDomain.CurrentDomain.AssemblyResolve -= ModAssemblyLoader.OnAssemblyResolve;
		StopValidationWatchers();
		EntriesByScope.Clear();
		ModAssemblyLoader.Clear();
		FailedModReasonsById.Clear();
		ModEnumDefinitions.Clear();
		HasPendingFileChanges = false;
		IsRefreshingOnFocus = false;
		Initialized = false;
		SteamWorkshopModsPath = null;
		Logger = null;
		OnDiscoveryUpdated = null;
	}

	public static bool AnyGameplayModsDiscovered()
	{
		if (EntriesByScope.ContainsKey(ModActivationScope.City))
		{
			List<DiscoveredModEntry> list = EntriesByScope[ModActivationScope.City];
			if (list != null && list.Count > 0)
			{
				return true;
			}
		}
		if (EntriesByScope.ContainsKey(ModActivationScope.Initialization))
		{
			List<DiscoveredModEntry> list = EntriesByScope[ModActivationScope.Initialization];
			if (list != null)
			{
				return list.Count > 0;
			}
			return false;
		}
		return false;
	}

	public static bool IsDiscovered(string modId)
	{
		foreach (List<DiscoveredModEntry> value in EntriesByScope.Values)
		{
			foreach (DiscoveredModEntry item in value)
			{
				if (item.ModId == modId)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsDiscovered(List<string> modIds)
	{
		HashSet<string> hashSet = new HashSet<string>(modIds);
		foreach (List<DiscoveredModEntry> value in EntriesByScope.Values)
		{
			foreach (DiscoveredModEntry item in value)
			{
				if (hashSet.Remove(item.ModId) && hashSet.Count == 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void Initialize()
	{
		if (!Initialized)
		{
			Logger = new UnityModLogger("[ModDiscovery] ");
			ModAssemblyLoader.Initialize(Logger);
			Directory.CreateDirectory(ModsLocalPath);
			SetupValidationWatchers();
			AppDomain.CurrentDomain.AssemblyResolve += ModAssemblyLoader.OnAssemblyResolve;
			Application.quitting += OnApplicationQuitting;
			Application.focusChanged += OnApplicationFocusChanged;
			Initialized = true;
		}
	}

	private static void OnApplicationQuitting()
	{
		try
		{
			Shutdown();
		}
		catch (Exception exception)
		{
			Logger?.Error("Failed while shutting down mod discovery during application quit.");
			Logger?.Error(exception);
		}
	}

	private static void Shutdown()
	{
		if (Initialized)
		{
			Application.quitting -= OnApplicationQuitting;
			Application.focusChanged -= OnApplicationFocusChanged;
			AppDomain.CurrentDomain.AssemblyResolve -= ModAssemblyLoader.OnAssemblyResolve;
			StopValidationWatchers();
			Clear();
			Initialized = false;
		}
	}

	public static async Task DiscoverAllModsAsync(Action<float> onProgress = null)
	{
		await DiscoverySemaphore.WaitAsync();
		try
		{
			onProgress?.Invoke(0f);
			Initialize();
			Clear();
			List<ModInfo> source = await GetAllModsAsync(onlyEnabledSteamMods: true);
			List<ModInfo> list = (from modInfo in source
				where modInfo.steamItemId != 0
				orderby modInfo.steamItemId
				select modInfo).ToList();
			if (list.Count > 0)
			{
				SteamWorkshopModsPath = Path.GetDirectoryName(list[0].modFolder);
			}
			List<ModInfo> localMods = source.Where((ModInfo modInfo) => modInfo.steamItemId == 0).OrderBy((ModInfo modInfo) => Path.GetFullPath(modInfo.modFolder), StringComparer.OrdinalIgnoreCase).ToList();
			onProgress?.Invoke(0.5f);
			await DiscoverFromInfosAsync(list, isSteamMod: true);
			onProgress?.Invoke(0.75f);
			await DiscoverFromInfosAsync(localMods, isSteamMod: false);
			SyncDiscoveredLocalizationPaths();
			onProgress?.Invoke(1f);
			SetupValidationWatchers();
			NotifyDiscoveryUpdated();
		}
		finally
		{
			DiscoverySemaphore.Release();
		}
	}

	public static async Task DiscoverSteamModsByIdsAsync(HashSet<ulong> steamModIds)
	{
		if (steamModIds == null || steamModIds.Count == 0)
		{
			return;
		}
		await DiscoverySemaphore.WaitAsync();
		try
		{
			Initialize();
			ServiceHelper.EnsureInitialized();
			List<ModInfo> list = await SteamModLoadingService.GetSubscribedMods();
			if (list != null)
			{
				List<ModInfo> list2 = (from modInfo in list
					where modInfo != null && modInfo.steamItemId != 0L && !string.IsNullOrWhiteSpace(modInfo.modFolder) && steamModIds.Contains(modInfo.steamItemId)
					orderby modInfo.steamItemId
					select modInfo).ToList();
				if (list2.Count > 0)
				{
					SteamWorkshopModsPath = Path.GetDirectoryName(list2[0].modFolder);
				}
				await DiscoverFromInfosAsync(list2, isSteamMod: true);
				SyncDiscoveredLocalizationPaths();
				SetupValidationWatchers();
				NotifyDiscoveryUpdated();
			}
		}
		finally
		{
			DiscoverySemaphore.Release();
		}
	}

	public static async Task<List<ModInfo>> GetAllModsAsync(bool onlyEnabledSteamMods = false)
	{
		Initialize();
		ServiceHelper.EnsureInitialized();
		ModManifest.ReloadFromDisk();
		List<ModInfo> list = await SteamModLoadingService.GetSubscribedMods();
		List<ModInfo> localMods = GetLocalMods();
		List<ModInfo> list2 = new List<ModInfo>();
		if (list != null)
		{
			foreach (ModInfo item in from modInfo in list
				where modInfo != null && !string.IsNullOrWhiteSpace(modInfo.modFolder)
				orderby modInfo.steamItemId
				select modInfo)
			{
				if (!onlyEnabledSteamMods || (item.steamItemId != 0L && ModManifest.Contains(item.steamItemId)))
				{
					list2.Add(item);
				}
			}
		}
		list2.AddRange(localMods.OrderBy((ModInfo modInfo) => Path.GetFullPath(modInfo.modFolder), StringComparer.OrdinalIgnoreCase));
		return list2;
	}

	public static IReadOnlyList<DiscoveredModEntry> GetEntries(ModActivationScope scope)
	{
		if (EntriesByScope.TryGetValue(scope, out var value))
		{
			return value;
		}
		return Array.Empty<DiscoveredModEntry>();
	}

	public static void RemoveDiscoveredSteamMods(HashSet<ulong> steamModIds)
	{
		if (steamModIds == null || steamModIds.Count == 0)
		{
			return;
		}
		HashSet<string> removedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (ulong steamModId in steamModIds)
		{
			removedModIds.Add(steamModId.ToString());
		}
		foreach (List<DiscoveredModEntry> value in EntriesByScope.Values)
		{
			value.RemoveAll(delegate(DiscoveredModEntry entry)
			{
				if (!removedModIds.Contains(entry.ModId))
				{
					return false;
				}
				ModAssemblyLoader.RemoveKnownModFolder(entry.ModFolder);
				return true;
			});
		}
		foreach (string item in removedModIds)
		{
			FailedModReasonsById.Remove(item);
			ModAssemblyLoader.RemoveTrackedAssemblyForMod(item);
		}
		SyncDiscoveredLocalizationPaths();
		NotifyDiscoveryUpdated();
	}

	private static void Clear()
	{
		EntriesByScope.Clear();
		ModAssemblyLoader.Clear();
		FailedModReasonsById.Clear();
		ModEnumDefinitions.Clear();
	}

	private static void SyncDiscoveredLocalizationPaths()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (List<DiscoveredModEntry> value2 in EntriesByScope.Values)
		{
			foreach (DiscoveredModEntry item in value2)
			{
				if (string.IsNullOrWhiteSpace(item.ModId) || string.IsNullOrWhiteSpace(item.ModFolder))
				{
					continue;
				}
				string text = Path.Combine(item.ModFolder, "Locales");
				if (Directory.Exists(text))
				{
					if (!dictionary.TryGetValue(item.ModId, out var value))
					{
						dictionary.Add(item.ModId, text);
					}
					else if (!string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
					{
						Logger?.Warn("Skipped duplicate localization path for mod '" + item.ModId + "' from '" + text + "' because '" + value + "' is already registered.");
					}
				}
			}
		}
		LocalizorManager.SyncExternalLocalizationPaths(dictionary);
	}

	private static async void OnApplicationFocusChanged(bool hasFocus)
	{
		if (!hasFocus || IsRefreshingOnFocus || !Initialized || !HasPendingFileChanges)
		{
			return;
		}
		IsRefreshingOnFocus = true;
		try
		{
			HasPendingFileChanges = false;
			await DiscoverAllModsAsync();
		}
		catch (Exception exception)
		{
			HasPendingFileChanges = true;
			Logger?.Error("Failed to refresh mod discovery on application focus.");
			Logger?.Error(exception);
		}
		finally
		{
			IsRefreshingOnFocus = false;
		}
	}

	private static void SetupValidationWatchers()
	{
		ModsLocalWatcher = ResetValidationWatcher(ModsLocalWatcher, ModsLocalPath);
		SteamWorkshopWatcher = ResetValidationWatcher(SteamWorkshopWatcher, SteamWorkshopModsPath);
	}

	private static void StopValidationWatchers()
	{
		if (ModsLocalWatcher != null)
		{
			ModsLocalWatcher.Dispose();
			ModsLocalWatcher = null;
		}
		if (SteamWorkshopWatcher != null)
		{
			SteamWorkshopWatcher.Dispose();
			SteamWorkshopWatcher = null;
		}
	}

	private static FileSystemWatcher ResetValidationWatcher(FileSystemWatcher watcher, string path)
	{
		if (watcher != null)
		{
			watcher.Dispose();
			watcher = null;
		}
		if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
		{
			return null;
		}
		watcher = new FileSystemWatcher(path)
		{
			IncludeSubdirectories = false,
			NotifyFilter = (NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite),
			Filter = "*",
			EnableRaisingEvents = true
		};
		watcher.Changed += OnValidationWatcherChanged;
		watcher.Created += OnValidationWatcherChanged;
		watcher.Deleted += OnValidationWatcherChanged;
		watcher.Renamed += OnValidationWatcherRenamed;
		return watcher;
	}

	private static void OnValidationWatcherChanged(object sender, FileSystemEventArgs args)
	{
		HasPendingFileChanges = true;
	}

	private static void OnValidationWatcherRenamed(object sender, RenamedEventArgs args)
	{
		HasPendingFileChanges = true;
	}

	private static void NotifyDiscoveryUpdated()
	{
		OnDiscoveryUpdated?.Invoke();
	}

	private static async Task DiscoverFromInfosAsync(IEnumerable<ModInfo> mods, bool isSteamMod)
	{
		List<(ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion)> pendingMods = new List<(ModInfo, string, string, string, string, Version)>();
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> modDisplayNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (ModInfo mod in mods)
		{
			if (mod == null || string.IsNullOrWhiteSpace(mod.modFolder))
			{
				continue;
			}
			string modKey = GetModKey(mod, isSteamMod);
			string modDisplayName = GetModDisplayName(mod, modKey);
			FailedModReasonsById.Remove(modKey);
			try
			{
				if (!TryGetRootDllPath(mod.modFolder, out var dllPath, out var errorMessage))
				{
					RegisterFailedMod(modKey, modDisplayName, errorMessage);
					Logger.Warn(errorMessage + " Mod folder: '" + mod.modFolder + "'.");
				}
				else
				{
					if (!ModEnumDefinitions.TryDefineModEnums(mod))
					{
						continue;
					}
					ModAssemblyLoader.AddKnownModFolder(mod.modFolder);
					AssemblyName assemblyName;
					try
					{
						assemblyName = AssemblyName.GetAssemblyName(dllPath);
					}
					catch (Exception exception)
					{
						RegisterFailedMod(modKey, modDisplayName, "The mod assembly metadata could not be read.");
						Logger.Error("Failed to read assembly metadata from '" + dllPath + "'.");
						Logger.Error(exception);
						goto end_IL_0098;
					}
					string text = assemblyName?.Name;
					if (string.IsNullOrWhiteSpace(text))
					{
						RegisterFailedMod(modKey, modDisplayName, "The mod assembly does not have a valid assembly name.");
						Logger.Warn("Skipped mod '" + modDisplayName + "' because '" + dllPath + "' has no assembly name.");
					}
					else
					{
						if (!dictionary.TryGetValue(text, out var value))
						{
							value = (dictionary[text] = new List<string>());
						}
						value.Add(modKey);
						modDisplayNamesById[modKey] = modDisplayName;
						pendingMods.Add((mod, modKey, modDisplayName, dllPath, text, assemblyName.Version));
					}
					continue;
				}
				end_IL_0098:;
			}
			catch (Exception exception2)
			{
				RegisterFailedMod(modKey, modDisplayName, "An unhandled error occurred during discovery.");
				Logger.Error("Unhandled error while discovering mod from folder '" + mod.modFolder + "'.");
				Logger.Error(exception2);
			}
		}
		HashSet<string> duplicateModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, List<string>> item in dictionary)
		{
			if (item.Value.Count < 2)
			{
				continue;
			}
			string[] value2 = item.Value.Select((string modId) => (!modDisplayNamesById.TryGetValue(modId, out var value3)) ? modId : value3).ToArray();
			string text2 = string.Join(", ", value2);
			string reason = "Duplicate mod assembly name detected. Assembly name '" + item.Key + "' is used by multiple mods: " + text2 + ".";
			foreach (string item2 in item.Value)
			{
				RegisterFailedMod(item2, modDisplayNamesById[item2], reason);
				duplicateModIds.Add(item2);
			}
		}
		pendingMods.RemoveAll(((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => duplicateModIds.Contains(tuple2.ModId));
		HashSet<string> failedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		(ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion)[] array = pendingMods.ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			(ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) candidate = array[num];
			if (!(await ModAssemblyLoader.TryLoadDependenciesAsync(candidate.ModInfo.modFolder, candidate.ModId, candidate.ModDisplayName, RegisterFailedMod)))
			{
				failedModIds.Add(candidate.ModId);
				pendingMods.Remove(candidate);
			}
		}
		Dictionary<string, string> modIdByAssemblySimpleName = pendingMods.ToDictionary(((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => tuple2.AssemblySimpleName, ((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => tuple2.ModId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Version> modAssemblyVersionBySimpleName = pendingMods.ToDictionary(((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => tuple2.AssemblySimpleName, ((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => tuple2.AssemblyVersion, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, (ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion)> pendingModsById = pendingMods.ToDictionary(((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) tuple2) => tuple2.ModId, ((ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) result) => result, StringComparer.OrdinalIgnoreCase);
		HashSet<string> loadedModIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (pendingModsById.Count > 0)
		{
			bool progressed = false;
			array = pendingModsById.Values.ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				(ModInfo ModInfo, string ModId, string ModDisplayName, string DllPath, string AssemblySimpleName, Version AssemblyVersion) candidate = array[num];
				try
				{
					Assembly assembly = await ModAssemblyLoader.LoadAssemblyFromPathAsync(candidate.DllPath, candidate.ModId);
					string deferredDependencyModId;
					if (assembly == null)
					{
						RegisterFailedMod(candidate.ModId, candidate.ModDisplayName, "The mod assembly could not be loaded.");
						Logger.Error("Failed to load assembly from '" + candidate.DllPath + "'.");
						failedModIds.Add(candidate.ModId);
						pendingModsById.Remove(candidate.ModId);
						progressed = true;
					}
					else if (!ModCompatibilityValidator.TryValidateModAssemblyCompatibility(assembly, candidate.ModId, candidate.ModDisplayName, modIdByAssemblySimpleName, modAssemblyVersionBySimpleName, loadedModIds, failedModIds, RegisterFailedMod, Logger, out deferredDependencyModId))
					{
						failedModIds.Add(candidate.ModId);
						pendingModsById.Remove(candidate.ModId);
						progressed = true;
					}
					else
					{
						if (!string.IsNullOrWhiteSpace(deferredDependencyModId))
						{
							continue;
						}
						if (!ModCompatibilityValidator.TryValidateGameAssemblyCompatibility(assembly, candidate.ModInfo.modFolder, candidate.ModId, candidate.ModDisplayName, ModAssemblyLoader.ModFolders, RegisterFailedMod, Logger))
						{
							failedModIds.Add(candidate.ModId);
							pendingModsById.Remove(candidate.ModId);
							progressed = true;
							continue;
						}
						foreach (Type registeredModType in GetRegisteredModTypes(assembly, candidate.DllPath))
						{
							bool flag = false;
							(ModActivationScope, Type)[] scopeAttributeMappings = ScopeAttributeMappings;
							for (int num2 = 0; num2 < scopeAttributeMappings.Length; num2++)
							{
								(ModActivationScope, Type) tuple = scopeAttributeMappings[num2];
								if (registeredModType.IsDefined(tuple.Item2, inherit: false))
								{
									RegisterEntry(registeredModType, candidate.ModId, candidate.ModInfo.modFolder, candidate.ModDisplayName, tuple.Item1);
									flag = true;
								}
							}
							if (!flag)
							{
								Logger.Warn("Registered mod type '" + registeredModType.FullName + "' in '" + Path.GetFileName(candidate.DllPath) + "' does not have any recognized mod entry scope attribute.");
							}
						}
						loadedModIds.Add(candidate.ModId);
						pendingModsById.Remove(candidate.ModId);
						progressed = true;
						continue;
					}
				}
				catch (Exception exception3)
				{
					RegisterFailedMod(candidate.ModId, candidate.ModDisplayName, "An unhandled error occurred during discovery.");
					Logger.Error("Unhandled error while discovering mod from folder '" + candidate.ModInfo.modFolder + "'.");
					Logger.Error(exception3);
					failedModIds.Add(candidate.ModId);
					pendingModsById.Remove(candidate.ModId);
					progressed = true;
				}
			}
			if (progressed)
			{
				continue;
			}
			foreach (var value4 in pendingModsById.Values)
			{
				RegisterFailedMod(value4.ModId, value4.ModDisplayName, "Mod dependencies could not be resolved. Possible circular dependency.");
				failedModIds.Add(value4.ModId);
			}
			pendingModsById.Clear();
		}
	}

	private static IEnumerable<Type> GetRegisteredModTypes(Assembly assembly, string dllPath)
	{
		RegisterModClassAttribute[] array;
		try
		{
			array = assembly.GetCustomAttributes<RegisterModClassAttribute>().ToArray();
		}
		catch (Exception exception)
		{
			Logger.Error("Failed to read registered mod classes from '" + Path.GetFileName(dllPath) + "'.");
			Logger.Error(exception);
			yield break;
		}
		if (array.Length == 0)
		{
			Logger.Warn("No RegisterModClassAttribute attributes were found in '" + Path.GetFileName(dllPath) + "'.");
			yield break;
		}
		HashSet<Type> yieldedTypes = new HashSet<Type>();
		RegisterModClassAttribute[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Type modClassType = array2[i].ModClassType;
			if (modClassType == null)
			{
				Logger.Warn("A RegisterModClassAttribute in '" + Path.GetFileName(dllPath) + "' had a null type.");
			}
			else if (modClassType.Assembly != assembly)
			{
				Logger.Warn("Registered mod type '" + modClassType.FullName + "' in '" + Path.GetFileName(dllPath) + "' does not belong to the mod assembly.");
			}
			else if (!modClassType.IsClass || modClassType.IsAbstract)
			{
				Logger.Warn("Registered mod type '" + modClassType.FullName + "' in '" + Path.GetFileName(dllPath) + "' must be a non-abstract class.");
			}
			else if (!typeof(IModBigAmbitions).IsAssignableFrom(modClassType))
			{
				Logger.Warn("Registered mod type '" + modClassType.FullName + "' in '" + Path.GetFileName(dllPath) + "' does not implement IModBigAmbitions.");
			}
			else if (yieldedTypes.Add(modClassType))
			{
				yield return modClassType;
			}
		}
	}

	private static List<DiscoveredModEntry> GetOrCreateEntries(ModActivationScope scope)
	{
		if (EntriesByScope.TryGetValue(scope, out var value))
		{
			return value;
		}
		value = new List<DiscoveredModEntry>();
		EntriesByScope[scope] = value;
		return value;
	}

	private static void RegisterEntry(Type type, string modId, string modFolder, string modDisplayName, ModActivationScope scope)
	{
		List<DiscoveredModEntry> orCreateEntries = GetOrCreateEntries(scope);
		if (!orCreateEntries.Any((DiscoveredModEntry entry) => entry.ModId == modId && entry.EntryType == type))
		{
			orCreateEntries.Add(new DiscoveredModEntry
			{
				ModId = modId,
				ModFolder = modFolder,
				ModDisplayName = modDisplayName,
				EntryType = type,
				Scope = scope
			});
		}
	}

	private static bool TryGetRootDllPath(string modFolder, out string dllPath, out string errorMessage)
	{
		dllPath = null;
		errorMessage = null;
		if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
		{
			errorMessage = "Mod not discovered. Mod folder does not exist.";
			return false;
		}
		string[] files = Directory.GetFiles(modFolder, "*.dll", SearchOption.TopDirectoryOnly);
		if (files.Length == 0)
		{
			errorMessage = "Mod not discovered. No DLL file was found in the mod root folder.";
			return false;
		}
		if (files.Length > 1)
		{
			errorMessage = "Mod not discovered. Multiple DLL files were found in the mod root folder. Expected exactly one mod DLL.";
			return false;
		}
		dllPath = files[0];
		return true;
	}

	private static string GetModKey(ModInfo modInfo, bool isSteamMod)
	{
		if (isSteamMod && modInfo.steamItemId != 0L)
		{
			return modInfo.steamItemId.ToString();
		}
		if (string.IsNullOrWhiteSpace(modInfo.modFolder))
		{
			return "0";
		}
		return Path.GetFullPath(modInfo.modFolder);
	}

	private static string GetModDisplayName(ModInfo modInfo, string modId)
	{
		if (string.IsNullOrWhiteSpace(modInfo.title))
		{
			return modId;
		}
		return modInfo.title;
	}

	private static List<ModInfo> GetLocalMods()
	{
		List<ModInfo> list = new List<ModInfo>();
		if (!Directory.Exists(ModsLocalPath))
		{
			return list;
		}
		string[] directories = Directory.GetDirectories(ModsLocalPath, "*", SearchOption.TopDirectoryOnly);
		foreach (string modFolder in directories)
		{
			if (IsValidLocalModFolder(modFolder, out var folderName))
			{
				list.Add(new ModInfo
				{
					modFolder = modFolder,
					title = folderName
				});
			}
		}
		return list;
	}

	private static bool TryGetFolderName(string folderPath, out string folderName)
	{
		folderName = null;
		if (string.IsNullOrWhiteSpace(folderPath))
		{
			return false;
		}
		folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		return !string.IsNullOrWhiteSpace(folderName);
	}

	private static bool IsValidLocalModFolder(string modFolder, out string folderName)
	{
		folderName = null;
		if (!TryGetFolderName(modFolder, out folderName))
		{
			return false;
		}
		return IsModFolder(modFolder);
	}

	private static bool IsModFolder(string modFolder)
	{
		if (string.IsNullOrWhiteSpace(modFolder) || !Directory.Exists(modFolder))
		{
			return false;
		}
		return Directory.GetFiles(modFolder, "*.dll", SearchOption.TopDirectoryOnly).Length == 1;
	}
}
