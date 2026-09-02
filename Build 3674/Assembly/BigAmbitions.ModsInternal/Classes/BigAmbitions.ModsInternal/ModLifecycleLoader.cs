// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModLifecycleLoader
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BAModAPI;
using BAModAPI.Services;
using BigAmbitions.ModsInternal;
using Localizor;
using UnityEngine;

public static class ModLifecycleLoader
{
	public const ModActivationScope LifetimeScope = ModActivationScope.Initialization;

	private static readonly Dictionary<ModActivationScope, List<ActiveModEntry>> ActiveEntriesByScope = new Dictionary<ModActivationScope, List<ActiveModEntry>>();

	public static readonly Dictionary<ModActivationScope, HashSet<string>> ActiveModIdsByScope = new Dictionary<ModActivationScope, HashSet<string>>();

	private static readonly Dictionary<string, string> FailedModMessagesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static bool IsInitialized;

	private static bool IsShuttingDown;

	private static ModActivationScope CurrentScope;

	public static IReadOnlyDictionary<string, string> FailedModMessages => FailedModMessagesById;

	public static bool AnyGameplayModsLoaded
	{
		get
		{
			if (!IsScopeLoaded(ModActivationScope.City))
			{
				return IsScopeLoaded(ModActivationScope.Initialization);
			}
			return true;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Application.quitting -= OnApplicationQuitting;
		IsShuttingDown = false;
		IsInitialized = false;
		ActiveEntriesByScope.Clear();
		ActiveModIdsByScope.Clear();
		FailedModMessagesById.Clear();
		CurrentScope = ModActivationScope.None;
	}

	public static void Initialize()
	{
		if (!IsInitialized)
		{
			Application.quitting += OnApplicationQuitting;
			IsInitialized = true;
		}
	}

	private static void OnApplicationQuitting()
	{
		try
		{
			UnloadAllScopesAsync().GetAwaiter().GetResult();
			AssetService.UnloadAllBundles(unloadAllLoadedObjects: true);
		}
		catch (Exception exception)
		{
			UnityModLogger unityModLogger = new UnityModLogger("[ModLifecycleLoader] ");
			unityModLogger.Error("Failed while unloading mods during application quit.");
			unityModLogger.Error(exception);
		}
	}

	public static async Task ApplyScopeChangesAsync(HashSet<ulong> addedModIds, HashSet<ulong> removedModIds)
	{
		bool num = removedModIds != null && removedModIds.Count > 0;
		bool flag = addedModIds != null && addedModIds.Count > 0;
		if (num || flag)
		{
			await UnloadChangedModEntriesAsync(BuildChangedModIdSet(addedModIds, removedModIds));
			await LoadAddedModEntriesAsync(addedModIds);
			SyncDiscoveryFailures();
			LocalizorManager.LoadLocalizationTables(reloadIndex: false);
		}
	}

	public static async Task LoadScopeAsync(ModActivationScope scope)
	{
		SyncDiscoveryFailures();
		if (!ModDiscoveryRegistry.Entries.ContainsKey(scope))
		{
			return;
		}
		bool isPersistentScope = scope == ModActivationScope.Initialization;
		if (IsScopeActive(scope, isPersistentScope))
		{
			return;
		}
		if (!isPersistentScope && !IsScopeLoaded(ModActivationScope.Initialization))
		{
			await LoadScopeAsync(ModActivationScope.Initialization);
		}
		if (!isPersistentScope && CurrentScope != ModActivationScope.None && CurrentScope != scope)
		{
			await UnloadScopeAsync(CurrentScope);
		}
		if (!isPersistentScope && !IsScopeLoaded(ModActivationScope.Initialization))
		{
			await LoadScopeAsync(ModActivationScope.Initialization);
		}
		await UnloadScopeAsync(scope);
		if (!isPersistentScope)
		{
			CurrentScope = scope;
		}
		List<ActiveModEntry> activeEntries = GetOrCreateActiveEntries(scope);
		HashSet<string> activeModIds = GetOrCreateActiveModIds(scope);
		foreach (DiscoveredModEntry entry in ModDiscoveryRegistry.GetEntries(scope))
		{
			await TryLoadDiscoveredEntryAsync(scope, entry, activeEntries, activeModIds);
		}
		LocalizorManager.LoadLocalizationTables(reloadIndex: false);
		ModEvents.onModsLoaded?.InvokeSafely();
	}

	public static async Task UnloadScopeAsync(ModActivationScope scope)
	{
		await UnloadScopeAsync(scope, unloadLifetimeWhenFolderMissing: true);
	}

	private static async Task UnloadScopeAsync(ModActivationScope scope, bool unloadLifetimeWhenFolderMissing)
	{
		if (scope == ModActivationScope.Initialization && CurrentScope != ModActivationScope.None && CurrentScope != ModActivationScope.Initialization)
		{
			await UnloadScopeAsync(CurrentScope, unloadLifetimeWhenFolderMissing: false);
		}
		if (!ActiveEntriesByScope.TryGetValue(scope, out var activeEntries) || activeEntries.Count == 0)
		{
			if (scope == CurrentScope)
			{
				CurrentScope = ModActivationScope.None;
			}
			return;
		}
		if (unloadLifetimeWhenFolderMissing && scope != ModActivationScope.Initialization && activeEntries.Exists((ActiveModEntry entry) => !IsModFolderAvailable(entry.ModFolder)))
		{
			await UnloadScopeAsync(ModActivationScope.Initialization, unloadLifetimeWhenFolderMissing: false);
		}
		HashSet<string> orCreateActiveModIds = GetOrCreateActiveModIds(scope);
		await UnloadMatchingEntriesAsync(activeEntries, orCreateActiveModIds, (ActiveModEntry _) => true, (ActiveModEntry _) => $"Failed while unloading scope {scope}.");
		if (scope == CurrentScope)
		{
			CurrentScope = ModActivationScope.None;
		}
		LocalizorManager.LoadLocalizationTables(reloadIndex: false);
		ModEvents.onModsUnloaded?.InvokeSafely();
	}

	private static async Task UnloadAllScopesAsync()
	{
		if (IsShuttingDown)
		{
			return;
		}
		IsShuttingDown = true;
		try
		{
			foreach (ModActivationScope value in Enum.GetValues(typeof(ModActivationScope)))
			{
				if (value != ModActivationScope.None && value != ModActivationScope.Initialization)
				{
					await UnloadScopeAsync(value);
				}
			}
			await UnloadScopeAsync(ModActivationScope.Initialization);
		}
		finally
		{
			IsShuttingDown = false;
		}
	}

	public static bool IsScopeLoaded(ModActivationScope scope)
	{
		if (ActiveEntriesByScope.TryGetValue(scope, out var value))
		{
			return value.Count > 0;
		}
		return false;
	}

	public static List<(string ModId, string ModDisplayName)> GetActiveMods()
	{
		HashSet<string> first = (ActiveModIdsByScope.TryGetValue(ModActivationScope.Initialization, out var value) ? value : new HashSet<string>());
		HashSet<string> second = (ActiveModIdsByScope.TryGetValue(CurrentScope, out var value2) ? value2 : new HashSet<string>());
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		AddActiveModsForScope(ModActivationScope.Initialization, dictionary);
		if (CurrentScope != ModActivationScope.Initialization)
		{
			AddActiveModsForScope(CurrentScope, dictionary);
		}
		foreach (string item in first.Union(second))
		{
			dictionary.TryAdd(item, item);
		}
		return dictionary.Select((KeyValuePair<string, string> mod) => (Key: mod.Key, Value: mod.Value)).ToList();
	}

	private static void AddActiveModsForScope(ModActivationScope scope, Dictionary<string, string> modsById)
	{
		if (!ActiveEntriesByScope.TryGetValue(scope, out var value))
		{
			return;
		}
		foreach (ActiveModEntry item in value)
		{
			if (!string.IsNullOrWhiteSpace(item.ModId))
			{
				modsById[item.ModId] = (string.IsNullOrWhiteSpace(item.ModDisplayName) ? item.ModId : item.ModDisplayName);
			}
		}
	}

	private static bool IsScopeActive(ModActivationScope scope, bool isPersistentScope)
	{
		if (!IsScopeLoaded(scope))
		{
			return false;
		}
		if (!isPersistentScope)
		{
			return CurrentScope == scope;
		}
		return true;
	}

	private static List<ActiveModEntry> GetOrCreateActiveEntries(ModActivationScope scope)
	{
		if (ActiveEntriesByScope.TryGetValue(scope, out var value))
		{
			return value;
		}
		value = new List<ActiveModEntry>();
		ActiveEntriesByScope[scope] = value;
		return value;
	}

	private static HashSet<string> GetOrCreateActiveModIds(ModActivationScope scope)
	{
		if (ActiveModIdsByScope.TryGetValue(scope, out var value))
		{
			return value;
		}
		value = new HashSet<string>(StringComparer.Ordinal);
		ActiveModIdsByScope[scope] = value;
		return value;
	}

	private static async Task TryLoadDiscoveredEntryAsync(ModActivationScope scope, DiscoveredModEntry discoveredEntry, List<ActiveModEntry> activeEntries, HashSet<string> activeModIds)
	{
		if (!IsModFolderAvailable(discoveredEntry.ModFolder))
		{
			TrackFailedMod(discoveredEntry.ModId, discoveredEntry.ModDisplayName, "The mod folder is missing. Loading is skipped until the folder is available again.");
			return;
		}
		if (!TryCreateModInstance(discoveredEntry, out var instance))
		{
			TrackFailedMod(discoveredEntry.ModId, discoveredEntry.ModDisplayName, "The mod class instance could not be created.");
			return;
		}
		UnityModLogger logger = CreateModLogger(discoveredEntry.ModDisplayName);
		ModContext context = new ModContext(discoveredEntry.ModFolder, discoveredEntry.ModId, logger);
		try
		{
			await LoadModInstanceAsync(instance, context, discoveredEntry.ModId);
			activeEntries.Add(CreateActiveEntry(discoveredEntry, instance));
			activeModIds.Add(discoveredEntry.ModId);
			FailedModMessagesById.Remove(discoveredEntry.ModId);
		}
		catch (Exception exception)
		{
			TrackFailedMod(discoveredEntry.ModId, discoveredEntry.ModDisplayName, "The mod failed during initialization. " + GetFailureDetails(exception));
			logger.Error($"Failed while loading '{discoveredEntry.EntryType.FullName}' for scope {scope}.");
			logger.Error(exception);
		}
	}

	private static bool TryCreateModInstance(DiscoveredModEntry discoveredEntry, out IModBigAmbitions instance)
	{
		try
		{
			instance = (IModBigAmbitions)Activator.CreateInstance(discoveredEntry.EntryType);
			return true;
		}
		catch (Exception exception)
		{
			UnityModLogger unityModLogger = CreateModLogger(discoveredEntry.ModDisplayName);
			unityModLogger.Error("Failed to instantiate '" + discoveredEntry.EntryType.FullName + "'.");
			unityModLogger.Error(exception);
			instance = null;
			return false;
		}
	}

	private static ActiveModEntry CreateActiveEntry(DiscoveredModEntry discoveredEntry, IModBigAmbitions instance)
	{
		return new ActiveModEntry
		{
			ModId = discoveredEntry.ModId,
			ModFolder = discoveredEntry.ModFolder,
			ModDisplayName = discoveredEntry.ModDisplayName,
			Scope = discoveredEntry.Scope,
			Instance = instance
		};
	}

	private static async Task UnloadMatchingEntriesAsync(List<ActiveModEntry> activeEntries, HashSet<string> activeModIds, Func<ActiveModEntry, bool> shouldUnload, Func<ActiveModEntry, string> errorMessageFactory)
	{
		for (int index = activeEntries.Count - 1; index >= 0; index--)
		{
			ActiveModEntry activeEntry = activeEntries[index];
			if (shouldUnload(activeEntry))
			{
				UnityModLogger logger = CreateModLogger(activeEntry.ModDisplayName);
				try
				{
					await UnloadModInstanceAsync(activeEntry.Instance, activeEntry.ModId);
					activeEntries.RemoveAt(index);
					activeModIds.Remove(activeEntry.ModId);
				}
				catch (Exception exception)
				{
					logger.Error(errorMessageFactory(activeEntry));
					logger.Error(exception);
				}
			}
		}
	}

	private static HashSet<string> BuildChangedModIdSet(HashSet<ulong> addedModIds, HashSet<ulong> removedModIds)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (addedModIds != null)
		{
			foreach (ulong addedModId in addedModIds)
			{
				hashSet.Add(addedModId.ToString());
			}
		}
		if (removedModIds != null)
		{
			foreach (ulong removedModId in removedModIds)
			{
				hashSet.Add(removedModId.ToString());
			}
		}
		return hashSet;
	}

	private static async Task UnloadChangedModEntriesAsync(HashSet<string> changedModIds)
	{
		if (changedModIds == null || changedModIds.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<ModActivationScope, List<ActiveModEntry>> item in ActiveEntriesByScope)
		{
			if (item.Value != null && item.Value.Count != 0)
			{
				ModActivationScope scope = item.Key;
				List<ActiveModEntry> value = item.Value;
				HashSet<string> orCreateActiveModIds = GetOrCreateActiveModIds(scope);
				await UnloadMatchingEntriesAsync(value, orCreateActiveModIds, (ActiveModEntry activeEntry) => changedModIds.Contains(activeEntry.ModId), (ActiveModEntry activeEntry) => $"Failed while unloading mod '{activeEntry.ModId}' in scope {scope}.");
			}
		}
	}

	private static async Task LoadAddedModEntriesAsync(HashSet<ulong> addedModIds)
	{
		if (addedModIds == null || addedModIds.Count == 0)
		{
			return;
		}
		HashSet<string> addedModIdStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (ulong addedModId in addedModIds)
		{
			addedModIdStrings.Add(addedModId.ToString());
		}
		await LoadAddedModEntriesForScopeAsync(ModActivationScope.Initialization, addedModIdStrings);
		if (ShouldTryLoadScope(CurrentScope) && CurrentScope != ModActivationScope.Initialization)
		{
			await LoadAddedModEntriesForScopeAsync(CurrentScope, addedModIdStrings);
		}
	}

	private static bool ShouldTryLoadScope(ModActivationScope scope)
	{
		if (scope == ModActivationScope.None)
		{
			return false;
		}
		if (scope == CurrentScope)
		{
			return true;
		}
		return IsScopeLoaded(scope);
	}

	private static async Task LoadAddedModEntriesForScopeAsync(ModActivationScope scope, HashSet<string> addedModIds)
	{
		if (!ModDiscoveryRegistry.Entries.ContainsKey(scope))
		{
			return;
		}
		List<ActiveModEntry> activeEntries = GetOrCreateActiveEntries(scope);
		HashSet<string> activeModIds = GetOrCreateActiveModIds(scope);
		foreach (DiscoveredModEntry entry in ModDiscoveryRegistry.GetEntries(scope))
		{
			if (addedModIds.Contains(entry.ModId) && !activeModIds.Contains(entry.ModId))
			{
				await TryLoadDiscoveredEntryAsync(scope, entry, activeEntries, activeModIds);
			}
		}
	}

	private static async Task LoadModInstanceAsync(IModBigAmbitions instance, ModContext context, string modId)
	{
		try
		{
			if (instance.RelativeAssetBundlePaths != null)
			{
				string[] relativeAssetBundlePaths = instance.RelativeAssetBundlePaths;
				foreach (string text in relativeAssetBundlePaths)
				{
					string bundleKey = text.Replace('\\', '/');
					string absolutePath = ResolveBundleAbsolutePath(context.ModRootPath, text);
					AssetService.RegisterBundle(modId, bundleKey, absolutePath);
				}
			}
			await instance.OnLoadAsync(context);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			try
			{
				await instance.OnUnloadAsync();
			}
			catch (Exception exception2)
			{
				Debug.LogException(exception2);
			}
			throw;
		}
	}

	private static async Task UnloadModInstanceAsync(IModBigAmbitions instance, string modId)
	{
		await instance.OnUnloadAsync();
	}

	private static string ResolveBundleAbsolutePath(string modRoot, string relativePath)
	{
		string text = Path.GetDirectoryName(relativePath) ?? string.Empty;
		string fileName = Path.GetFileName(relativePath);
		string currentPlatformFolderName = GetCurrentPlatformFolderName();
		string path = (string.IsNullOrEmpty(text) ? Path.Combine(currentPlatformFolderName, fileName) : Path.Combine(text, currentPlatformFolderName, fileName));
		string text2 = Path.Combine(modRoot, path);
		if (File.Exists(text2))
		{
			return text2;
		}
		return Path.Combine(modRoot, relativePath);
	}

	private static string GetCurrentPlatformFolderName()
	{
		switch (Application.platform)
		{
		case RuntimePlatform.WindowsPlayer:
		case RuntimePlatform.WindowsEditor:
			return "Windows";
		case RuntimePlatform.OSXEditor:
		case RuntimePlatform.OSXPlayer:
			return "Mac";
		case RuntimePlatform.LinuxPlayer:
		case RuntimePlatform.LinuxEditor:
			return "Linux";
		default:
			return "Windows";
		}
	}

	private static UnityModLogger CreateModLogger(string modDisplayName)
	{
		return new UnityModLogger("[Mod:" + modDisplayName + "] ");
	}

	private static string GetFailureDetails(Exception exception)
	{
		if (exception == null)
		{
			return "Unknown error.";
		}
		string message = exception.Message;
		if (string.IsNullOrWhiteSpace(message))
		{
			return exception.GetType().Name + ".";
		}
		message = message.Replace(Environment.NewLine, " ").Trim();
		if (message.Length > 200)
		{
			message = message.Substring(0, 200).TrimEnd() + "...";
		}
		return exception.GetType().Name + ": " + message;
	}

	private static void SyncDiscoveryFailures()
	{
		FailedModMessagesById.Clear();
		foreach (KeyValuePair<string, string> failedModReason in ModDiscoveryRegistry.FailedModReasons)
		{
			FailedModMessagesById[failedModReason.Key] = failedModReason.Value;
		}
	}

	private static void TrackFailedMod(string modId, string modDisplayName, string reason)
	{
		if (!string.IsNullOrWhiteSpace(modId))
		{
			FailedModMessagesById[modId] = modDisplayName + ": " + reason;
		}
	}

	private static bool IsModFolderAvailable(string modFolder)
	{
		if (!string.IsNullOrWhiteSpace(modFolder))
		{
			return Directory.Exists(modFolder);
		}
		return false;
	}
}
