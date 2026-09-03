using System;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Customization;

public class BodyValues : MonoBehaviour
{
	[SerializeField]
	private CharacterCustomizer controller;

	[SerializeField]
	private Slider strengthSlider;

	[SerializeField]
	private Slider fatnessSlider;

	public Action onAppearanceChange;

	private void Awake()
	{
		strengthSlider.onValueChanged.AddListener(ChangeStrength);
		fatnessSlider.onValueChanged.AddListener(ChangeFatness);
	}

	public void Show()
	{
		strengthSlider.SetValueWithoutNotify(controller.appearanceSetter.data.strength);
		fatnessSlider.SetValueWithoutNotify(controller.appearanceSetter.data.fatness);
		base.gameObject.SetActive(value: true);
	}

	private void ChangeStrength(float value)
	{
		controller.appearanceSetter.data.strength = value;
		controller.appearanceSetter.UpdateVisuals();
		onAppearanceChange?.Invoke();
	}

	private void ChangeFatness(float value)
	{
		controller.appearanceSetter.data.fatness = value;
		controller.appearanceSetter.UpdateVisuals();
		onAppearanceChange?.Invoke();
	}

	public void ChangeStrengthSlider(float value)
	{
		strengthSlider.SetValueWithoutNotify(value);
		ChangeStrength(value);
	}

	public void ChangeFatnessSlider(float value)
	{
		fatnessSlider.SetValueWithoutNotify(value);
		ChangeFatness(value);
	}
}
