using System;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Customization;

public class Gender : MonoBehaviour
{
	[SerializeField]
	private CharacterCustomizer controller;

	[SerializeField]
	private Transform container;

	[NonSerialized]
	public GenderButton currentButton;

	private void Start()
	{
		foreach (Transform item in container)
		{
			GenderButton component = item.GetComponent<GenderButton>();
			if (component.gender == controller.appearanceSetter.data.gender)
			{
				component.GetComponent<Image>().sprite = component.selectedIcon;
				currentButton = component;
				break;
			}
		}
	}

	public void SelectGender(GenderButton button)
	{
		if (!(currentButton == button))
		{
			currentButton.GetComponent<Image>().sprite = currentButton.unselectedIcon;
			button.GetComponent<Image>().sprite = button.selectedIcon;
			currentButton = button;
			controller.appearanceSetter.data.gender = button.gender;
			controller.appearanceSetter.UpdateVisuals();
		}
	}
}
