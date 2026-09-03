using System;
using BigAmbitions.InputSystem;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace City.CityMap;

public class CityMapFilter : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private Image toggleOnImage;

	[SerializeField]
	private TMP_Text labelText;

	[SerializeField]
	private Sprite alternativeToggleSprite;

	[SerializeField]
	private Button focusButton;

	public CityMapFilterCategory category;

	private string _filterName;

	private Action<bool> _onToggle;

	private bool _excludeFromSelection;

	private Vector3? _focusPoint;

	private Func<Vector3?> _focusPointResolver;

	public Toggle Toggle => toggle;

	public CityMapFilterData Data { get; private set; }

	public void SetUp(string filterName, Action<bool> onToggle, Sprite filterIcon, CityMapFilterData data, bool excludeFromSelection = false, LanguageChangeEventDataHolder languageData = default(LanguageChangeEventDataHolder), Vector3? focusPoint = null, Func<Vector3?> focusPointResolver = null)
	{
		_filterName = filterName;
		_onToggle = onToggle;
		_excludeFromSelection = excludeFromSelection;
		Data = data;
		_focusPoint = focusPoint;
		_focusPointResolver = focusPointResolver;
		if (string.IsNullOrEmpty(languageData.Key))
		{
			label.Key = filterName;
		}
		else
		{
			label.SetData(languageData);
		}
		icon.sprite = filterIcon;
		icon.enabled = filterIcon != null;
		if (_focusPoint.HasValue || _focusPointResolver != null)
		{
			focusButton.gameObject.SetActive(value: true);
			focusButton.onClick.RemoveListener(OnFocusButtonClick);
			focusButton.onClick.AddListener(OnFocusButtonClick);
		}
		else
		{
			focusButton.gameObject.SetActive(value: false);
		}
	}

	public bool IsAvailable()
	{
		if (Data?.isAvailable != null)
		{
			return Data.isAvailable();
		}
		return true;
	}

	public bool MatchesSearch(string searchText)
	{
		if (!string.IsNullOrWhiteSpace(searchText))
		{
			return labelText.text.IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	public void OnButtonClick()
	{
		toggle.isOn = !toggle.isOn;
	}

	public void OnToggleClick(bool isOn)
	{
		PlayerAction.Click.Reset();
		if (!_excludeFromSelection)
		{
			SaveGameManager.Current.SelectedCitymapFilters.Remove(_filterName);
			if (isOn)
			{
				SaveGameManager.Current.SelectedCitymapFilters.Add(_filterName);
			}
		}
		_onToggle?.Invoke(isOn);
		if ((bool)category)
		{
			category.UpdateToggleAllState();
		}
	}

	private void OnFocusButtonClick()
	{
		Vector3? vector = _focusPointResolver?.Invoke() ?? _focusPoint;
		if (vector.HasValue)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.cityMapCam.MoveCameraToTarget(vector.Value);
		}
	}

	public void SetLabelColor(Color color)
	{
		labelText.color = color;
	}

	public void SetLabelAlignment(TextAlignmentOptions alignment)
	{
		labelText.alignment = alignment;
	}

	public void ApplyAlternativeToggleSprite()
	{
		toggleOnImage.sprite = alternativeToggleSprite;
	}
}
