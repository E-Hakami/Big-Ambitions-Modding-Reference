using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

public class SliderToLabel : MonoBehaviour
{
	public TextLocalizationComponent label;

	private Slider _slider;

	public string localizationKey;

	private void Start()
	{
		_slider = GetComponent<Slider>();
		ValueChanged(_slider.value);
		_slider.onValueChanged.AddListener(ValueChanged);
	}

	public void ValueChanged(float value)
	{
		label.SetData(LanguageChangeEventDataHolder.Create(localizationKey, new
		{
			value = Math.Round(value)
		}));
	}
}
