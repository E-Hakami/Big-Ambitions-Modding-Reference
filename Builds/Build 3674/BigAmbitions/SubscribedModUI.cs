using System;
using System.Collections.Generic;
using BigAmbitions.ModsInternal;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions;

public class SubscribedModUI : SingleModUI
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text descriptionLabel;

	[SerializeField]
	private Button viewOnSteamButton;

	[SerializeField]
	private BasicTooltip modConflictsTooltip;

	[SerializeField]
	private TextLocalizationComponent modConflictsLabel;

	[SerializeField]
	private TMP_Text versionLabel;

	[SerializeField]
	private TextLocalizationComponent modEnabledLabel;

	[SerializeField]
	private Toggle modEnabledToggle;

	private bool _hasConflicts;

	private ModInfo _modInfo;

	public Action onToggle;

	private void Awake()
	{
		viewOnSteamButton.onClick.AddListener(OnViewOnSteamClick);
		modEnabledToggle.onValueChanged.AddListener(OnModToggleValueChanged);
	}

	private void OnDestroy()
	{
		viewOnSteamButton.onClick.RemoveListener(OnViewOnSteamClick);
		modEnabledToggle.onValueChanged.RemoveListener(OnModToggleValueChanged);
	}

	public override void Setup(ModInfo modInfo)
	{
		base.Setup(modInfo);
		_modInfo = modInfo;
		titleLabel.text = modInfo.title;
		descriptionLabel.text = modInfo.description;
		bool flag = modInfo.steamItemId != 0;
		viewOnSteamButton.interactable = flag;
		versionLabel.text = (flag ? GameVersion.GetVersionString(modInfo.targetBuildNumber) : "main_menu_mods_local_mod".GetLocalization());
		modEnabledToggle.isOn = !flag || ModManifest.Contains(currentSteamItemId);
		UpdateConflicts();
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
	}

	public void UpdateConflicts()
	{
		List<string> modConflictsList = ModEnumDefinitions.GetModConflictsList(_modInfo);
		_hasConflicts = modConflictsList != null && modConflictsList.Count > 0;
		modConflictsTooltip.gameObject.SetActive(_hasConflicts);
		if (_hasConflicts)
		{
			modConflictsLabel.SetData(new LanguageChangeEventDataHolder
			{
				Key = "main_menu_mods_conflicts_label",
				Arguments = new
				{
					number = modConflictsList.Count
				}
			});
			modConflictsTooltip.localizationArguments = new
			{
				modList = string.Join("\n", modConflictsList)
			};
			if (_modInfo.steamItemId != 0L && ModManifest.Contains(currentSteamItemId))
			{
				ModManifest.Remove(currentSteamItemId);
			}
		}
		bool flag = _modInfo.steamItemId != 0;
		bool isOn = !_hasConflicts && (!flag || ModManifest.Contains(currentSteamItemId));
		modEnabledToggle.isOn = isOn;
		modEnabledToggle.interactable = flag && !_hasConflicts;
		UpdateEnabledLabel(isOn);
	}

	private void OnViewOnSteamClick()
	{
		SteamHelper.OpenSteamWithWorkshopItem(currentSteamItemId);
	}

	private void OnModToggleValueChanged(bool isOn)
	{
		if (_modInfo != null && _modInfo.steamItemId != 0L && !_hasConflicts)
		{
			UpdateEnabledLabel(isOn);
			if (!isOn)
			{
				ModManifest.Remove(currentSteamItemId);
			}
			else
			{
				ModManifest.Add(currentSteamItemId);
			}
			onToggle?.Invoke();
		}
	}

	private void UpdateEnabledLabel(bool isOn)
	{
		modEnabledLabel.Key = (isOn ? "main_menu_mods_mod_active" : "main_menu_mods_mod_inactive");
	}
}
