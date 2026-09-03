using System;
using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class DropdownSelectorUI : MonoBehaviour
{
	[SerializeField]
	private GameObject container;

	[SerializeField]
	private UI.Elements.Dropdown dropdown;

	[SerializeField]
	private Button confirmButton;

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private TextLocalizationComponent bodyLabel;

	private Action<string> _onConfirm;

	private void Awake()
	{
		dropdown.onOptionSelected.AddListener(OnOptionSelected);
		confirmButton.onClick.AddListener(OnConfirm);
		confirmButton.interactable = dropdown.SelectedOptionIndex != -1;
		container.SetActive(value: false);
	}

	private void Start()
	{
		DropdownSelector.onShow = (Action<List<string>, Action<string>, string, string>)Delegate.Combine(DropdownSelector.onShow, new Action<List<string>, Action<string>, string, string>(Show));
	}

	private void OnDestroy()
	{
		DropdownSelector.onShow = (Action<List<string>, Action<string>, string, string>)Delegate.Remove(DropdownSelector.onShow, new Action<List<string>, Action<string>, string, string>(Show));
		DropdownSelector.isOpen = false;
	}

	public void Show(List<string> localizedOptions, Action<string> onConfirm, string headerKey = null, string bodyKey = null)
	{
		bodyLabel.gameObject.SetActive(!string.IsNullOrEmpty(bodyKey));
		headerLabel.Key = headerKey;
		bodyLabel.Key = bodyKey;
		_onConfirm = onConfirm;
		dropdown.SetOptions(localizedOptions, localize: false);
		container.SetActive(value: true);
		DropdownSelector.isOpen = true;
	}

	private void OnOptionSelected(int index)
	{
		confirmButton.interactable = index != -1;
	}

	private void OnConfirm()
	{
		_onConfirm?.Invoke(dropdown.SelectedOption);
		container.SetActive(value: false);
		DropdownSelector.isOpen = false;
	}

	public void OnCancel()
	{
		if (DropdownSelector.isOpen)
		{
			_onConfirm = null;
			container.gameObject.SetActive(value: false);
			DropdownSelector.isOpen = false;
		}
	}
}
