using System;
using Blueprints;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

public class ModBackupWarningUi : MonoBehaviour
{
	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private RectTransform panel;

	[SerializeField]
	private UI.Components.InputField newSaveNameInput;

	[SerializeField]
	private Button confirmButton;

	private bool _isOpen;

	private Action<string> _onConfirmAction;

	private void Start()
	{
		container.gameObject.SetActive(value: false);
	}

	public void Show(Action<string> onConfirmAction, string defaultSaveName = "")
	{
		newSaveNameInput.SetText(defaultSaveName);
		_onConfirmAction = onConfirmAction;
		container.gameObject.SetActive(value: true);
		_isOpen = true;
	}

	public void ClickCancel()
	{
		if (_isOpen)
		{
			_onConfirmAction = null;
			container.gameObject.SetActive(value: false);
			_isOpen = false;
		}
	}

	public void ClickConfirm()
	{
		if (_isOpen)
		{
			container.gameObject.SetActive(value: false);
			_isOpen = false;
			_onConfirmAction?.Invoke(newSaveNameInput.GetRawValue());
		}
	}

	public void OnSaveNameInputChanged(string newValue)
	{
		newValue = FileSystemHelper.MakeValidFilename(newValue);
		confirmButton.interactable = !string.IsNullOrWhiteSpace(newValue);
	}
}
