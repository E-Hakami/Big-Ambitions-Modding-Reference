using System.Collections.Generic;
using BigAmbitions.Mods;
using UnityEngine;

namespace BigAmbitions.ModsInternal;

internal static class ModOptionPrefs
{
	private static readonly HashSet<string> WrittenKeys = new HashSet<string>();

	public static void ClearAllForOptions(string modId, IEnumerable<ModOption> options)
	{
		if (options == null)
		{
			return;
		}
		foreach (ModOption option in options)
		{
			if (option is IPersistableOption persistableOption && !string.IsNullOrEmpty(persistableOption.Id))
			{
				Clear(modId, persistableOption.Id);
			}
		}
	}

	public static bool Load(string modId, string optionId, bool defaultValue)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			return UnityEngine.PlayerPrefs.GetInt(text, defaultValue ? 1 : 0) != 0;
		}
		return defaultValue;
	}

	public static int Load(string modId, string optionId, int defaultValue)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			return UnityEngine.PlayerPrefs.GetInt(text, defaultValue);
		}
		return defaultValue;
	}

	public static float Load(string modId, string optionId, float defaultValue)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			return UnityEngine.PlayerPrefs.GetFloat(text, defaultValue);
		}
		return defaultValue;
	}

	public static void Save(string modId, string optionId, bool value)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			UnityEngine.PlayerPrefs.SetInt(text, value ? 1 : 0);
			WrittenKeys.Add(text);
		}
	}

	public static void Save(string modId, string optionId, int value)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			UnityEngine.PlayerPrefs.SetInt(text, value);
			WrittenKeys.Add(text);
		}
	}

	public static void Save(string modId, string optionId, float value)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			UnityEngine.PlayerPrefs.SetFloat(text, value);
			WrittenKeys.Add(text);
		}
	}

	public static void Clear(string modId, string optionId)
	{
		string text = BuildKey(modId, optionId);
		if (text != null)
		{
			UnityEngine.PlayerPrefs.DeleteKey(text);
			WrittenKeys.Remove(text);
		}
	}

	private static string BuildKey(string modId, string optionId)
	{
		if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(optionId))
		{
			return null;
		}
		return "m:" + modId + ":" + optionId;
	}
}
