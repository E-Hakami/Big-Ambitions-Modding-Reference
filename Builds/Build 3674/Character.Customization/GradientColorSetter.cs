using UI.Components;
using UnityEngine;

namespace Character.Customization;

[RequireComponent(typeof(GradientSlider))]
public class GradientColorSetter : MonoBehaviour
{
	[SerializeField]
	private CharacterCustomizer controller;

	[SerializeField]
	private GradientSlider gradientSlider;

	private void Awake()
	{
		gradientSlider = GetComponent<GradientSlider>();
	}

	private void Start()
	{
		gradientSlider.OnValueChangedAddListener(ChangeColor);
	}

	private void ChangeColor(float value)
	{
		controller.appearanceSetter.data.color = gradientSlider.GetColor();
		controller.appearanceSetter.UpdateVisuals();
		controller.onAppearanceChange?.Invoke();
	}
}
