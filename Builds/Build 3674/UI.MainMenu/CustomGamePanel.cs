using System.Collections.Generic;
using BigAmbitions.GameAnalytics;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class CustomGamePanel : MonoBehaviour
{
	[SerializeField]
	private Button startButton;

	[SerializeField]
	private CustomGameOptionsHandler optionsHandler;

	[SerializeField]
	private UI.Elements.Dropdown presetDropdown;

	[SerializeField]
	private CustomGameNameInput nameInput;

	private void Start()
	{
		CustomGamePresetsHandler.FetchPresets();
		if ((bool)startButton)
		{
			startButton.onClick.AddListener(StartCustomGame);
		}
		optionsHandler.onValueChanged.AddListener(SelectEqualPresetOrUnsaved);
		presetDropdown.onOptionSelected.AddListener(LoadPreset);
		presetDropdown.SetPlaceholder("Unsaved", localize: false);
		UpdateDropdown();
		if ((bool)startButton)
		{
			presetDropdown.SelectOption(0);
		}
	}

	private void OnEnable()
	{
		if ((bool)startButton)
		{
			startButton.interactable = true;
		}
	}

	private void StartCustomGame()
	{
		if (!(NewGamePanel.GameMode != "CustomGameMode"))
		{
			NewGamePanel.MainMenuController.StartNewGame(CustomGameOptionsHandler.GetPreset());
		}
	}

	private void UpdateDropdown(string presetName = null)
	{
		List<string> list = new List<string>();
		DifficultySetting[] difficultySettings = InstanceBehavior<GlobalReferences>.Instance.difficultySettings;
		foreach (DifficultySetting difficultySetting in difficultySettings)
		{
			list.Add(difficultySetting.key.Localize().ToString() + " *");
		}
		list.AddRange(CustomGamePresetsHandler.PresetNames);
		presetDropdown.SetOptions(list, localize: false);
		if (!string.IsNullOrEmpty(presetName))
		{
			presetDropdown.SelectOption(presetName);
		}
		else
		{
			presetDropdown.SelectOption(-1);
		}
		presetDropdown.onOptionSelected.Invoke(presetDropdown.SelectedOptionIndex);
	}

	private void SelectEqualPresetOrUnsaved()
	{
		GameVariables preset = CustomGameOptionsHandler.GetPreset();
		int num = -1;
		int num2 = InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Length;
		for (int i = 0; i < num2; i++)
		{
			if (preset.EqualsValues(InstanceBehavior<GlobalReferences>.Instance.difficultySettings[i].ToGameVariables()))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			for (int j = 0; j < CustomGamePresetsHandler.Presets.Count; j++)
			{
				if (preset.EqualsValues(CustomGamePresetsHandler.Presets[j]))
				{
					num = j + num2;
					break;
				}
			}
		}
		presetDropdown.SelectOption(num);
	}

	private void LoadPreset(int presetIndex)
	{
		if (presetIndex >= 0)
		{
			int num = InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Length;
			if (presetIndex < CustomGamePresetsHandler.Presets.Count + num)
			{
				GameVariables preset = ((presetIndex < num) ? InstanceBehavior<GlobalReferences>.Instance.difficultySettings[presetIndex].ToGameVariables() : CustomGamePresetsHandler.Presets[presetIndex - num]);
				optionsHandler.SetPreset(preset);
			}
		}
	}

	public void SavePreset()
	{
		nameInput.SetUp(delegate(string presetName)
		{
			GameVariables preset = CustomGameOptionsHandler.GetPreset();
			CustomGamePresetsHandler.SavePreset(presetName, preset);
			UpdateDropdown(presetName);
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "name", presetName } };
			Notifications.Show(NotificationType.Success, "custom_game_presets_saved_preset", notificationData);
			GameAnalytics.TrackUsedDifficultyPreset();
		});
	}

	public void DeletePreset()
	{
		if (presetDropdown.SelectedOptionIndex < InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Length)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "name", presetDropdown.SelectedOption } };
			Notifications.Show(NotificationType.Error, "custom_game_presets_cannot_delete_default", notificationData);
			return;
		}
		LanguageChangeEventDataHolder bodyData = "custom_game_presets_are_you_sure_delete".Localize(new
		{
			name = presetDropdown.SelectedOption
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			int index = presetDropdown.SelectedOptionIndex - InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Length;
			string text = CustomGamePresetsHandler.PresetNames[index];
			CustomGamePresetsHandler.DeletePreset(text);
			UpdateDropdown();
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "name", text } };
			Notifications.Show(NotificationType.Success, "custom_game_presets_deleted_preset", notificationData2);
			GameAnalytics.TrackUsedDifficultyPreset();
		});
	}

	public void CopyPresetValues()
	{
		CustomGamePresetsHandler.CopyToClipboard(CustomGameOptionsHandler.GetPreset());
		Notifications.Show(NotificationType.Success, "custom_game_presets_copied_to_clipboard");
		GameAnalytics.TrackUsedDifficultyPreset();
	}

	public void SetPresetValuesFromCurrent()
	{
		optionsHandler.SetPreset(SaveGameManager.Current.gameVariables);
		optionsHandler.preventAutoSetOnStart = true;
	}

	public void PastePresetValues()
	{
		GameVariables preset = CustomGamePresetsHandler.GetFromClipboard();
		if (preset == null)
		{
			Notifications.Show(NotificationType.Error, "custom_game_presets_paste_error");
			return;
		}
		optionsHandler.SetPreset(preset);
		nameInput.SetUp(delegate(string presetName)
		{
			CustomGamePresetsHandler.SavePreset(presetName, optionsHandler.EnsureLimits(preset));
			UpdateDropdown(presetName);
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "name", presetName } };
			Notifications.Show(NotificationType.Success, "custom_game_presets_pasted_preset", notificationData);
			GameAnalytics.TrackUsedDifficultyPreset();
		});
	}
}
