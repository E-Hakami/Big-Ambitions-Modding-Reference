using System;
using BAModAPI;
using BigAmbitions.ModsInternal;
using UnityEngine;
using UnityEngine.CrashReportHandler;

public static class CrashMetadata
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Register()
	{
		ModEvents.onModsLoaded = (Action)Delegate.Remove(ModEvents.onModsLoaded, new Action(UpdateModsActive));
		ModEvents.onModsLoaded = (Action)Delegate.Combine(ModEvents.onModsLoaded, new Action(UpdateModsActive));
		ModEvents.onModsUnloaded = (Action)Delegate.Remove(ModEvents.onModsUnloaded, new Action(UpdateModsActive));
		ModEvents.onModsUnloaded = (Action)Delegate.Combine(ModEvents.onModsUnloaded, new Action(UpdateModsActive));
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		UpdateModsActive();
		Set("building_address", "none");
	}

	public static void Set(string key, object value)
	{
		CrashReportHandler.SetUserMetadata(key, value?.ToString() ?? "null");
	}

	private static void UpdateModsActive()
	{
		Set("mods_active", ModLifecycleLoader.GetActiveMods().Count);
	}

	private static void OnEnterBuilding(Address address)
	{
		Set("building_address", address);
	}

	private static void OnExitBuilding(Address address)
	{
		Set("building_address", "none");
	}
}
