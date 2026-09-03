using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerPrefs
{
	private static readonly Dictionary<PlayerPref, object> cache = new Dictionary<PlayerPref, object>();

	private static readonly Dictionary<PlayerPref, object> defaultDictionary = new Dictionary<PlayerPref, object>
	{
		{
			PlayerPref.GlobalVolume,
			1f
		},
		{
			PlayerPref.RadioVolume,
			0.3f
		},
		{
			PlayerPref.RadioStation,
			1
		},
		{
			PlayerPref.MenuMusicVolume,
			0.25f
		},
		{
			PlayerPref.SfxVolume,
			1f
		},
		{
			PlayerPref.AiStoreMusicVolume,
			1f
		},
		{
			PlayerPref.InvertRotation,
			false
		},
		{
			PlayerPref.antiAliasingSetting,
			4
		},
		{
			PlayerPref.hbaoQuality,
			2
		},
		{
			PlayerPref.MaxAutoSavesPerGame,
			3
		},
		{
			PlayerPref.uiZooming,
			1f
		},
		{
			PlayerPref.MinutesBetweenAutoSaves,
			5
		},
		{
			PlayerPref.Locale,
			"en"
		},
		{
			PlayerPref.ShowWelcomeScreen,
			true
		},
		{
			PlayerPref.GameSpeed,
			1f
		},
		{
			PlayerPref.vSyncAndFPSLimitV2,
			7
		},
		{
			PlayerPref.ShowDataTrackingPopup,
			true
		},
		{
			PlayerPref.showFps,
			true
		},
		{
			PlayerPref.SteeringAssist,
			true
		},
		{
			PlayerPref.SeasonalDecorations,
			true
		},
		{
			PlayerPref.ControlHints,
			true
		},
		{
			PlayerPref.particleQuality,
			-1
		},
		{
			PlayerPref.textureQuality,
			-1
		},
		{
			PlayerPref.shadows,
			2
		}
	};

	public static event Action<PlayerPref> Changed;

	public static void SetInt(PlayerPref pref, int value)
	{
		UnityEngine.PlayerPrefs.SetInt(pref.ToStringFast(), value);
		if (!cache.TryAdd(pref, value))
		{
			cache[pref] = value;
		}
		Changed?.Invoke(pref);
	}

	public static void SetFloat(PlayerPref pref, float value)
	{
		UnityEngine.PlayerPrefs.SetFloat(pref.ToStringFast(), value);
		if (!cache.TryAdd(pref, value))
		{
			cache[pref] = value;
		}
		Changed?.Invoke(pref);
	}

	public static void SetString(PlayerPref pref, string value)
	{
		UnityEngine.PlayerPrefs.SetString(pref.ToStringFast(), value);
		if (!cache.TryAdd(pref, value))
		{
			cache[pref] = value;
		}
		Changed?.Invoke(pref);
	}

	public static void SetBool(PlayerPref pref, bool value)
	{
		UnityEngine.PlayerPrefs.SetInt(pref.ToStringFast(), value ? 1 : 0);
		if (!cache.TryAdd(pref, value))
		{
			cache[pref] = value;
		}
		Changed?.Invoke(pref);
	}

	public static int GetInt(PlayerPref pref)
	{
		if (defaultDictionary.TryGetValue(pref, out var value))
		{
			return GetInt(pref, (int)value);
		}
		return GetInt(pref, 0);
	}

	public static float GetFloat(PlayerPref pref)
	{
		if (defaultDictionary.TryGetValue(pref, out var value))
		{
			return GetFloat(pref, (float)value);
		}
		return GetFloat(pref, 0f);
	}

	public static string GetString(PlayerPref pref)
	{
		if (defaultDictionary.TryGetValue(pref, out var value))
		{
			return GetString(pref, (string)value);
		}
		return GetString(pref, "");
	}

	public static bool GetBool(PlayerPref pref)
	{
		if (defaultDictionary.TryGetValue(pref, out var value))
		{
			return GetBool(pref, (bool)value);
		}
		return GetBool(pref, overrideDefaultValue: false);
	}

	private static int GetInt(PlayerPref pref, int overrideDefaultValue)
	{
		if (cache.TryGetValue(pref, out var value))
		{
			return (int)value;
		}
		return UnityEngine.PlayerPrefs.GetInt(pref.ToStringFast(), overrideDefaultValue);
	}

	private static float GetFloat(PlayerPref pref, float overrideDefaultValue)
	{
		if (cache.TryGetValue(pref, out var value))
		{
			return (float)value;
		}
		return UnityEngine.PlayerPrefs.GetFloat(pref.ToStringFast(), overrideDefaultValue);
	}

	private static string GetString(PlayerPref pref, string overrideDefaultValue)
	{
		if (cache.TryGetValue(pref, out var value))
		{
			return (string)value;
		}
		return UnityEngine.PlayerPrefs.GetString(pref.ToStringFast(), overrideDefaultValue);
	}

	private static bool GetBool(PlayerPref pref, bool overrideDefaultValue)
	{
		if (cache.TryGetValue(pref, out var value))
		{
			return (bool)value;
		}
		return UnityEngine.PlayerPrefs.GetInt(pref.ToStringFast(), overrideDefaultValue ? 1 : 0) == 1;
	}

	public static void DeleteKey(PlayerPref pref)
	{
		if (cache.ContainsKey(pref))
		{
			cache.Remove(pref);
		}
		UnityEngine.PlayerPrefs.DeleteKey(pref.ToStringFast());
	}

	public static bool HasKey(PlayerPref pref)
	{
		if (!cache.ContainsKey(pref))
		{
			return UnityEngine.PlayerPrefs.HasKey(pref.ToStringFast());
		}
		return true;
	}
}
