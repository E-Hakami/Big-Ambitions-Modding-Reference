using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions;

public class ChartFilterEntry : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private TextLocalizationComponent localizationComponent;

	[SerializeField]
	private Sprite selectedSprite;

	[SerializeField]
	private Sprite unselectedSprite;

	private FilterOption _correspondingFilterOption;

	private Action<string> _onPressed;

	public void Initialize(FilterOption filterOption, Action<string> onPressed)
	{
		_correspondingFilterOption = filterOption;
		_onPressed = onPressed;
		localizationComponent.SetData(LanguageChangeEventDataHolder.Create(_correspondingFilterOption.label));
		button.onClick.AddListener(ButtonPressed);
		base.gameObject.SetActive(value: true);
	}

	private void ButtonPressed()
	{
		_onPressed?.Invoke(_correspondingFilterOption.value);
	}

	public void SetSelected(bool isSelected)
	{
		button.image.sprite = (isSelected ? selectedSprite : unselectedSprite);
	}

	private void OnDestroy()
	{
		button.onClick.RemoveListener(ButtonPressed);
	}
}
