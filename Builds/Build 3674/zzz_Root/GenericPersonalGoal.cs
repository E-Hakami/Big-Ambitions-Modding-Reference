using System;
using System.Collections.Generic;
using System.IO;
using Extensions;
using HGAttributes;
using Helpers;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using Newtonsoft.Json;
using Steamworks.Data;
using UnityEngine;

public abstract class GenericPersonalGoal : ScriptableObject
{
	private const string SteamAchievementAutocompleteKey = "SteamAchievements";

	[BoxGroup("Generic")]
	[ValidateInput("HasTitle", null)]
	public string title;

	[BoxGroup("Generic")]
	public string progress;

	[BoxGroup("Generic")]
	public Sprite icon;

	[BoxGroup("Generic")]
	public string tierIdentifier;

	[BoxGroup("Generic")]
	[ReadOnly]
	public string identifier;

	[BoxGroup("Generic")]
	public string happinessModifierType = "ba:happinessmodifier_completed_personal_goal";

	[BoxGroup("Generic")]
	public List<string> triggers = new List<string>();

	[BoxGroup("Generic")]
	[Expandable]
	public List<Reward> rewards;

	[BoxGroup("Steam Achievements")]
	public bool usesSteamAchievements;

	[BoxGroup("Steam Achievements")]
	[ShowIf("usesSteamAchievements")]
	[Tooltip("if this goal is hidden it is not shown in Persona app")]
	public bool isHidden;

	[BoxGroup("Steam Achievements")]
	[ShowIf("usesSteamAchievements")]
	[ValidateInput("ValidateSteamAchievementID", null)]
	[AutocompleteDropdown("SteamAchievements")]
	public string steamAchievementID;

	[BoxGroup("Steam Achievements")]
	[ShowIf("usesSteamAchievements")]
	public bool usesSettingStats;

	[BoxGroup("Steam Achievements")]
	[ShowIf("IsUsingStats")]
	[ValidateInput("ValidateStatID", null)]
	public string steamStatID;

	[BoxGroup("Steam Achievements")]
	[ShowIf("IsUsingStats")]
	public bool showAchievementProgress;

	private Achievement _achievement;

	private int _lastFrameIndicated = -1;

	public bool IsCompleted => SaveGameManager.Current.completedPersonalGoals.Contains(identifier);

	[AutocompleteProvider("SteamAchievements")]
	private static string[] SteamAchievementNames => GetAllAchievementNames_EDITOR();

	public virtual float GetSortValue()
	{
		return float.MaxValue;
	}

	private void Awake()
	{
		if (usesSteamAchievements)
		{
			_achievement = new Achievement(steamAchievementID);
		}
	}

	private void OnValidate()
	{
		identifier = base.name.GenerateSlug();
	}

	protected virtual bool IsInt()
	{
		return false;
	}

	public void CheckForCompletion()
	{
		bool num = CheckIfCompleted();
		if (usesSteamAchievements && usesSettingStats)
		{
			if (ShouldIndicateProgress(out var current, out var max) && SteamAPI.isRunningAndValid && SteamAPI.StatsRecieved)
			{
				SteamAPI.IndicateProgress(steamAchievementID, current, max);
			}
			if (IsInt())
			{
				SteamAPI.SetState(steamStatID, Mathf.FloorToInt(GetSettingsStateValue()));
			}
			else
			{
				SteamAPI.SetState(steamStatID, Mathf.Floor(GetSettingsStateValue()));
			}
		}
		if (num)
		{
			SetCompleted();
		}
	}

	protected abstract bool CheckIfCompleted();

	protected virtual bool ShouldIndicateProgress(out int current, out int max)
	{
		current = -1;
		max = -1;
		bool result = usesSettingStats && showAchievementProgress && _lastFrameIndicated != Time.frameCount;
		_lastFrameIndicated = Time.frameCount;
		return result;
	}

	private void SetCompleted()
	{
		SaveGameManager.Current.completedPersonalGoals.Add(identifier);
		if (!isHidden)
		{
			InstanceBehavior<PersonalGoalOverlay>.Instance.ShowPersonalGoalCompleted(this);
			HappinessHelper.AddModifier(happinessModifierType);
		}
		if (Singleton<SteamAPI>.Instance.steamApiEnabled && usesSteamAchievements)
		{
			SteamAPI.SetAchievement(new Achievement(steamAchievementID));
		}
	}

	public virtual LanguageChangeEventDataHolder GetTitle()
	{
		return LanguageChangeEventDataHolder.Create(title);
	}

	public bool HasProgress()
	{
		return !string.IsNullOrEmpty(progress);
	}

	public virtual LanguageChangeEventDataHolder GetProgress()
	{
		return LanguageChangeEventDataHolder.Create(progress);
	}

	protected virtual float GetSettingsStateValue()
	{
		return 0f;
	}

	public bool IsCompletedOnSteam()
	{
		if (!SteamAPI.isRunningAndValid || !usesSteamAchievements)
		{
			return false;
		}
		return SteamAPI.GetAchievement(new Achievement(steamAchievementID));
	}

	public void ForceUpdateOnSteam()
	{
		if (!SteamAPI.isRunningAndValid)
		{
			return;
		}
		if (usesSettingStats)
		{
			if (IsInt())
			{
				SteamAPI.SetState(steamStatID, Mathf.RoundToInt(GetSettingsStateValue()));
			}
			else
			{
				SteamAPI.SetState(steamStatID, GetSettingsStateValue());
			}
		}
		else
		{
			_achievement = new Achievement(steamAchievementID);
			SteamAPI.SetAchievement(_achievement);
		}
	}

	private bool HasTitle()
	{
		if (!(title != ""))
		{
			return isHidden;
		}
		return true;
	}

	private static string[] GetAllAchievementNames_EDITOR()
	{
		string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..\\", "steamAchievements.json"));
		if (File.Exists(fullPath))
		{
			return JsonConvert.DeserializeObject<string[]>(File.ReadAllText(fullPath));
		}
		return new string[0];
	}

	private bool SteamAchievementExists_Editor()
	{
		return Array.IndexOf(GetAllAchievementNames_EDITOR(), steamAchievementID) >= 0;
	}

	private bool ValidateSteamAchievementID()
	{
		if (usesSteamAchievements)
		{
			return steamAchievementID != "";
		}
		return false;
	}

	private bool ValidateStatID()
	{
		if (usesSteamAchievements && usesSettingStats)
		{
			return steamStatID != "";
		}
		return false;
	}

	private bool IsUsingStats()
	{
		if (usesSteamAchievements)
		{
			return usesSettingStats;
		}
		return false;
	}
}
