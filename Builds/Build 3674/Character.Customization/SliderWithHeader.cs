using System;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Customization;

public class SliderWithHeader : MonoBehaviour
{
	[SerializeField]
	private TMP_Text headerField;

	[SerializeField]
	private Slider slider;

	public void SetUp(string headerKey, float value, float minValue, float maxValue, Action<float> onValueChanged)
	{
		headerField.text = headerKey.GetLocalization();
		slider.minValue = minValue;
		slider.maxValue = maxValue;
		slider.value = value;
		slider.onValueChanged.AddListener(onValueChanged.Invoke);
	}
}
