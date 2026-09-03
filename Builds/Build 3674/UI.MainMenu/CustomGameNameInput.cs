using System;
using System.Linq;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.MainMenu;

public class CustomGameNameInput : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private Button confirmButton;

	private UnityAction<string> _onConfirm;

	private void OnEnable()
	{
		inputField.text = string.Empty;
		OnValueChanged(string.Empty);
	}

	private void Start()
	{
		inputField.onValueChanged.AddListener(OnValueChanged);
	}

	public void SetUp(UnityAction<string> onConfirm)
	{
		_onConfirm = onConfirm;
		base.gameObject.SetActive(value: true);
	}

	public void OnConfirm()
	{
		bool num = CustomGamePresetsHandler.PresetNames.Any((string x) => string.Equals(x, inputField.text, StringComparison.CurrentCultureIgnoreCase));
		Action action = delegate
		{
			_onConfirm?.Invoke(inputField.text);
			base.gameObject.SetActive(value: false);
		};
		if (num)
		{
			LanguageChangeEventDataHolder bodyData = "custom_game_presets_preset_already_exists".Localize(new
			{
				name = inputField.text
			});
			Action onConfirmAction = action;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			action();
		}
	}

	private void OnValueChanged(string value)
	{
		confirmButton.interactable = !string.IsNullOrWhiteSpace(value);
	}
}
