// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.OptionsService
using System;
using System.Collections.Generic;
using BigAmbitions.Mods;
using UnityEngine;

public static class OptionsService
{
	private static readonly Dictionary<string, ModOptions> Entries = new Dictionary<string, ModOptions>();

	public static IReadOnlyDictionary<string, ModOptions> RegisteredEntries => Entries;

	public static event Action OnChanged;

	public static event Action OnReset;

	public static void Register(string modId, ModOptions options)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ModOption option in options.Options)
		{
			option.ModId = modId;
			if (option is IPersistableOption persistableOption)
			{
				if (string.IsNullOrEmpty(persistableOption.Id))
				{
					Debug.LogError("[Mod '" + modId + "'] " + option.GetType().Name + " '" + option.Label + "' has no id, its value will not persist.");
				}
				else if (!hashSet.Add(persistableOption.Id))
				{
					Debug.LogError("[Mod '" + modId + "'] Duplicate option id '" + persistableOption.Id + "', saved values will collide.");
				}
			}
		}
		Entries[modId] = options;
		OnChanged?.Invoke();
	}

	public static void RemoveModOptions(string modId)
	{
		if (Entries.Remove(modId))
		{
			OnChanged?.Invoke();
		}
	}

	public static void ResetAllToDefaults()
	{
		OnReset?.Invoke();
		OnChanged?.Invoke();
	}
}
