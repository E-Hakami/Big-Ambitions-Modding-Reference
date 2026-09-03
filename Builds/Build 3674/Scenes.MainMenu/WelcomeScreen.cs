using System;
using System.Collections.Generic;
using System.Linq;
using Localizor;
using Services;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class WelcomeScreen : MonoBehaviour
{
	[SerializeField]
	private UI.Elements.Dropdown languageDropdown;

	[SerializeField]
	private TMP_InputField emailInputField;

	[SerializeField]
	private Toggle trackingToggle;

	private void Start()
	{
		SetUpLanguageDropdown();
		SetUpTrackingToggle();
	}

	public void StartGame()
	{
		string text = emailInputField.text;
		if (!string.IsNullOrWhiteSpace(text))
		{
			PlayerPrefSettings.PlayerEmail = text;
			InstanceBehavior<MainMenuController>.Instance.StartCoroutine(NewsletterService.Subscribe(text));
		}
		PlayerPrefSettings.ShowWelcomeScreen = false;
		PlayerPrefSettings.ShowDataTrackingPopup = false;
		base.gameObject.SetActive(value: false);
		InstanceBehavior<MainMenuController>.Instance.NextMainMenuAction();
	}

	private void SetUpLanguageDropdown()
	{
		string[] availableLanguages = LocalizorManager.GetAvailableLanguages();
		int selectedOption = Array.FindIndex(availableLanguages, (string x) => x == LocalizorManager.LoadedLocale);
		List<string> newOptions = availableLanguages.Select((string x) => LocalizorManager.GetAvailableLanguagesPrettified(x) ?? "").ToList();
		languageDropdown.SetOptions(newOptions, localize: false, selectedOption);
		languageDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			LocalizorManager.SetUsedLanguage(PlayerPrefSettings.Locale = availableLanguages[optionIndex]);
		});
	}

	private void SetUpTrackingToggle()
	{
		trackingToggle.isOn = PlayerPrefSettings.allowTracking;
		trackingToggle.onValueChanged.AddListener(delegate(bool value)
		{
			PlayerPrefSettings.allowTracking = value;
		});
	}
}
