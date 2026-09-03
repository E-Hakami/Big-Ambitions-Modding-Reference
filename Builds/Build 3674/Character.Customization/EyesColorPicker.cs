using UI.Components;
using UnityEngine;

namespace Character.Customization;

public class EyesColorPicker : MonoBehaviour
{
	[SerializeField]
	private CharacterCustomizer controller;

	[SerializeField]
	private GradientSlider gradientSlider;

	public void Initialize()
	{
		gradientSlider.OnValueChangedAddListener(ChangeColor);
	}

	private void ChangeColor(float value)
	{
		controller.appearanceSetter.data.eyesColor = gradientSlider.GetColor();
		controller.appearanceSetter.UpdateVisuals();
		controller.onAppearanceChange?.Invoke();
	}
}
