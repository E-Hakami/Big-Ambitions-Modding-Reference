public static class PlayerPrefSettings
{
	public static int ControlMode
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.ControlMode);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.ControlMode, value);
		}
	}

	public static bool InvertRotation
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.InvertRotation);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.InvertRotation, value);
		}
	}

	public static bool RunByDefaultIndoors
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.RunByDefaultIndoors);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.RunByDefaultIndoors, value);
		}
	}

	public static bool VehicleMouseInput
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.VehicleMouseInput);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.VehicleMouseInput, value);
		}
	}

	public static bool SteeringAssist
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.SteeringAssist);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.SteeringAssist, value);
		}
	}

	public static float RadioVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.RadioVolume);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.RadioVolume, value);
		}
	}

	public static float GlobalVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.GlobalVolume);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.GlobalVolume, value);
		}
	}

	public static float MenuMusicVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.MenuMusicVolume);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.MenuMusicVolume, value);
		}
	}

	public static float SfxVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.SfxVolume);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.SfxVolume, value);
		}
	}

	public static float AiStoreMusicVolume
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.AiStoreMusicVolume);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.AiStoreMusicVolume, value);
		}
	}

	public static string Locale
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.Locale);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.Locale, value);
		}
	}

	public static float uiZooming
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.uiZooming);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.uiZooming, value);
		}
	}

	public static bool use12h
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.use12h);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.use12h, value);
		}
	}

	public static bool useImperial
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.useImperial);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.useImperial, value);
		}
	}

	public static float GameSpeed
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.GameSpeed);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.GameSpeed, value);
		}
	}

	public static string NumberFormat
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.NumberFormat);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.NumberFormat, value);
		}
	}

	public static bool allowTracking
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.allowTracking);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.allowTracking, value);
		}
	}

	public static bool SeasonalDecorations
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.SeasonalDecorations);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.SeasonalDecorations, value);
		}
	}

	public static bool ControlHints
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.ControlHints);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.ControlHints, value);
		}
	}

	public static int RadioStation
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.RadioStation);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.RadioStation, value);
		}
	}

	public static int MaxAutoSavesPerGame
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.MaxAutoSavesPerGame);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.MaxAutoSavesPerGame, value);
		}
	}

	public static int MinutesBetweenAutoSaves
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.MinutesBetweenAutoSaves);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.MinutesBetweenAutoSaves, value);
		}
	}

	public static int antiAliasingSetting
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.antiAliasingSetting);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.antiAliasingSetting, value);
		}
	}

	public static int vSyncAndFPSLimitV2
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.vSyncAndFPSLimitV2);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.vSyncAndFPSLimitV2, value);
		}
	}

	public static bool LowDetailCityMap
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.LowDetailCityMap);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.LowDetailCityMap, value);
		}
	}

	public static int hbaoQuality
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.hbaoQuality);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.hbaoQuality, value);
		}
	}

	public static bool showFps
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.showFps);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.showFps, value);
		}
	}

	public static int particleQuality
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.particleQuality);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.particleQuality, value);
		}
	}

	public static int textureQuality
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.textureQuality);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.textureQuality, value);
		}
	}

	public static float gamma
	{
		get
		{
			return PlayerPrefs.GetFloat(PlayerPref.gamma);
		}
		set
		{
			PlayerPrefs.SetFloat(PlayerPref.gamma, value);
		}
	}

	public static int shadows
	{
		get
		{
			return PlayerPrefs.GetInt(PlayerPref.shadows);
		}
		set
		{
			PlayerPrefs.SetInt(PlayerPref.shadows, value);
		}
	}

	public static string LastSaveGameName
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.LastSaveGameName);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.LastSaveGameName, value);
		}
	}

	public static bool ShowWelcomeScreen
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.ShowWelcomeScreen);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.ShowWelcomeScreen, value);
		}
	}

	public static string PlayerEmail
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.PlayerEmail);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.PlayerEmail, value);
		}
	}

	public static bool shownSystemRequirementWarning
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.shownSystemRequirementWarning);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.shownSystemRequirementWarning, value);
		}
	}

	public static bool ShowDataTrackingPopup
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.ShowDataTrackingPopup);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.ShowDataTrackingPopup, value);
		}
	}

	public static string LastPlayedVersion
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.LastPlayedVersion);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.LastPlayedVersion, value);
		}
	}

	public static string LatestCrashDate
	{
		get
		{
			return PlayerPrefs.GetString(PlayerPref.LatestCrashDate);
		}
		set
		{
			PlayerPrefs.SetString(PlayerPref.LatestCrashDate, value);
		}
	}

	public static bool HasAutoSubscribedBlueprints09
	{
		get
		{
			return PlayerPrefs.GetBool(PlayerPref.HasAutoSubscribedBlueprints09);
		}
		set
		{
			PlayerPrefs.SetBool(PlayerPref.HasAutoSubscribedBlueprints09, value);
		}
	}
}
