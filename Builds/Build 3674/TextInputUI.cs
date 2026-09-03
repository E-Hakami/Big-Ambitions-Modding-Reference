using System;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

public class TextInputUI : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private TextLocalizationComponent placeholder;

	[SerializeField]
	private TextLocalizationComponent buttonText;

	[SerializeField]
	private Button confirmButton;

	private Action _onClose;

	private Action<string> _onConfirm;

	private void Awake()
	{
		KeyboardInputHelper.Configure(inputField, delegate
		{
			if (confirmButton.interactable)
			{
				OnConfirm();
			}
		});
	}

	public void Show(string placeholderKey, string buttonTextKey, Action<string> onConfirm, Action onClose = null, string initialText = "", bool allowEmpty = false, int characterLimit = 999)
	{
		_onClose = onClose;
		_onConfirm = onConfirm;
		base.gameObject.SetActive(value: true);
		placeholder.Key = placeholderKey;
		buttonText.Key = buttonTextKey;
		inputField.text = initialText;
		inputField.characterLimit = characterLimit;
		inputField.onValueChanged.RemoveAllListeners();
		if (allowEmpty)
		{
			confirmButton.interactable = true;
			return;
		}
		confirmButton.interactable = !string.IsNullOrEmpty(initialText);
		inputField.onValueChanged.AddListener(delegate(string val)
		{
			confirmButton.interactable = !string.IsNullOrEmpty(val);
		});
	}

	public void OnConfirm()
	{
		base.gameObject.SetActive(value: false);
		_onConfirm?.Invoke(inputField.text);
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
		_onClose?.Invoke();
	}
}
