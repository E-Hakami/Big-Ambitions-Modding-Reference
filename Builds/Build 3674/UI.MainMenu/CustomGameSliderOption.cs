using System;
using System.Globalization;
using JimmysUnityUtilities;
using Localizor;
using NaughtyAttributes;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class CustomGameSliderOption : CustomGameOption<float>
{
	[Header("Slider")]
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private TMP_Text label;

	[SerializeField]
	private float minValue;

	[SerializeField]
	private float maxValue = 100f;

	[Header("Buttons")]
	[SerializeField]
	private HoldRepeatButton increaseButton;

	[SerializeField]
	private HoldRepeatButton decreaseButton;

	[SerializeField]
	private float buttonStepSize = 1f;

	[Header("Presentation")]
	[SerializeField]
	private bool asPercentage;

	[ShowIf("asPercentage")]
	[SerializeField]
	private bool isNormalized;

	[ShowIf("asPercentage")]
	[SerializeField]
	private bool isRaw;

	[ShowIf("asPercentage")]
	[SerializeField]
	private bool isNegative;

	[ShowIf("asPercentage")]
	[SerializeField]
	private bool isDecimal;

	[HideIf("asPercentage")]
	[SerializeField]
	private string localizationKey = "common_placeholder";

	private bool _isSliderInitialized;

	public float MinValue => minValue;

	public float MaxValue => maxValue;

	protected override void Awake()
	{
		base.Awake();
		InitializeSlider();
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(UpdateLabel));
	}

	private void InitializeSlider()
	{
		if (!_isSliderInitialized)
		{
			_isSliderInitialized = true;
			slider.minValue = minValue;
			slider.maxValue = maxValue;
			slider.wholeNumbers = !isNormalized;
			slider.onValueChanged.AddListener(delegate(float value)
			{
				UpdateLabel(value);
				UpdateButtons(value);
				onValueChanged?.Invoke(value);
			});
		}
	}

	private void UpdateLabel()
	{
		UpdateLabel(slider.value);
	}

	private void UpdateButtons(float value)
	{
		increaseButton.button.interactable = value < maxValue;
		decreaseButton.button.interactable = value > minValue;
	}

	private void UpdateLabel(float value)
	{
		string text = value.ToString(CultureInfo.InvariantCulture);
		if (asPercentage)
		{
			localizationKey = "common_placeholder";
			float num = (isNormalized ? (value * 100f) : (isRaw ? value : (value / maxValue * 100f)));
			string text2 = (isDecimal ? "0.0" : "0");
			text = num.ToString(text2) + "%";
			if (isNegative && !Mathf.Approximately(value, 0f))
			{
				text = "-" + text;
			}
		}
		label.text = localizationKey.Localize(new
		{
			data = text,
			years = text,
			value = text
		}).ToString();
	}

	public override void SetValue(float value)
	{
		InitializeSlider();
		value = Mathf.Clamp(value, minValue, maxValue);
		slider.SetValueWithoutNotify(value);
		UpdateLabel(value);
		UpdateButtons(value);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			slider.SetValueWithoutNotify(value);
			UpdateLabel(value);
			UpdateButtons(value);
			onValueChanged?.Invoke(value);
		});
	}

	public void IncreaseValue()
	{
		float num = slider.value + buttonStepSize;
		if (num > maxValue)
		{
			num = maxValue;
		}
		SetValue(num);
	}

	public void DecreaseValue()
	{
		float num = slider.value - buttonStepSize;
		if (num < minValue)
		{
			num = minValue;
		}
		SetValue(num);
	}
}
