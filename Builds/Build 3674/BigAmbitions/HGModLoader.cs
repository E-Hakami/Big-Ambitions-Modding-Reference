using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BAModAPI;
using BigAmbitions.ModsInternal;
using JimmysUnityUtilities;
using UI;
using UnityEngine;

namespace BigAmbitions;

public class HGModLoader : MonoBehaviour
{
	[SerializeField]
	private ModsNotification modsNotification;

	private static int PlaySessionId;

	private int _initializedPlaySessionId = -1;

	private int _initializingPlaySessionId = -1;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		PlaySessionId++;
	}

	private void Awake()
	{
		TryInitializeModsForPlaySession();
	}

	private void OnEnable()
	{
		TryInitializeModsForPlaySession();
	}

	private void OnDisable()
	{
		SteamAPI.OnSteamInit = (Action)Delegate.Remove(SteamAPI.OnSteamInit, new Action(OnSteamInitialized));
	}

	private void OnDestroy()
	{
		SteamAPI.OnSteamInit = (Action)Delegate.Remove(SteamAPI.OnSteamInit, new Action(OnSteamInitialized));
	}

	private void OnSteamInitialized()
	{
		if (_initializedPlaySessionId != PlaySessionId && _initializingPlaySessionId != PlaySessionId)
		{
			InitializeMods();
		}
	}

	private async Task InitializeMods()
	{
		if (_initializedPlaySessionId == PlaySessionId || _initializingPlaySessionId == PlaySessionId)
		{
			return;
		}
		_initializingPlaySessionId = PlaySessionId;
		try
		{
			ModLifecycleLoader.Initialize();
			await ModDiscoveryRegistry.DiscoverAllModsAsync();
			CheckConflicts();
			await ModLifecycleLoader.LoadScopeAsync(ModActivationScope.Initialization);
			await ModLifecycleLoader.LoadScopeAsync(ModActivationScope.MainMenu);
			LogFailedMods();
			_initializedPlaySessionId = PlaySessionId;
		}
		finally
		{
			_initializingPlaySessionId = -1;
		}
	}

	private void TryInitializeModsForPlaySession()
	{
		if (IsPlaySessionActive())
		{
			if (SteamAPI.isRunningAndValid)
			{
				OnSteamInitialized();
				return;
			}
			SteamAPI.OnSteamInit = (Action)Delegate.Remove(SteamAPI.OnSteamInit, new Action(OnSteamInitialized));
			SteamAPI.OnSteamInit = (Action)Delegate.Combine(SteamAPI.OnSteamInit, new Action(OnSteamInitialized));
		}
	}

	private static bool IsPlaySessionActive()
	{
		return Application.isPlaying;
	}

	private void CheckConflicts()
	{
		List<string> conflicts = ModEnumDefinitions.GetAllConflictingModsList();
		if (conflicts.Count != 0)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				modsNotification.Show(string.Join("\n", conflicts));
			});
		}
	}

	private static void LogFailedMods()
	{
		string[] array = ModLifecycleLoader.FailedModMessages.Values.OrderBy((string message) => message, StringComparer.OrdinalIgnoreCase).ToArray();
		if (array.Length != 0)
		{
			string text = string.Join(Environment.NewLine + "- ", array);
			Debug.LogWarning("Mods were not loaded:" + Environment.NewLine + "- " + text);
		}
	}
}
