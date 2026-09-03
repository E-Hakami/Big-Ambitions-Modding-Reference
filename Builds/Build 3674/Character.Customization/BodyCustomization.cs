using System.Collections.Generic;
using BigAmbitions.Characters;
using Intro;
using UI.Components;
using UnityEngine;

namespace Character.Customization;

public class BodyCustomization : MonoBehaviour
{
	public Sprite[] bodies;

	[SerializeField]
	private IntroCharacterCustomizer controller;

	[SerializeField]
	private GradientSlider skinColorGradientSlider;

	[SerializeField]
	private GradientSlider eyesColorGradientSlider;

	private Transform _container;

	private void Start()
	{
		Show();
	}

	public void Show()
	{
		controller.onMenuSelected?.Invoke();
		controller.characterZoom.ResetZoom();
		controller.characterNamePicker.gameObject.SetActive(value: true);
		controller.bodyValues.Show();
		List<(int, Sprite, bool)> list = new List<(int, Sprite, bool)>();
		for (int i = 0; i < bodies.Length; i++)
		{
			list.Add((i, bodies[i], false));
		}
		controller.bodyPicker.SetList(list, SelectBody, (int)controller.appearanceSetter.data.gender);
		controller.appearanceColorPicker.Hide();
	}

	private void SelectBody(int bodyIndex)
	{
		float fatness = controller.appearanceSetter.data.fatness;
		float strength = controller.appearanceSetter.data.strength;
		controller.appearanceSetter.Blendshapes.ResetAllBlendShapes();
		controller.appearanceSetter.SetRandomAppearance((BigAmbitions.Characters.Gender)bodyIndex, controller.Tags);
		controller.appearanceSetter.data.fatness = fatness;
		controller.appearanceSetter.data.strength = strength;
		controller.appearanceSetter.SetBody();
		eyesColorGradientSlider.RandomizeColor();
		skinColorGradientSlider.RandomizeColor();
		Show();
	}

	public void RandomizeCurrentGender()
	{
		controller.appearanceSetter.SetRandomAppearance(controller.appearanceSetter.data.gender, controller.Tags);
		eyesColorGradientSlider.RandomizeColor();
		skinColorGradientSlider.RandomizeColor();
		Show();
	}

	public void SetBodyIcon(int index, Sprite sprite)
	{
		if (index >= bodies.Length)
		{
			Sprite[] array = new Sprite[index + 1];
			for (int i = 0; i < bodies.Length; i++)
			{
				array[i] = bodies[i];
			}
			bodies = array;
		}
		bodies[index] = sprite;
	}
}
