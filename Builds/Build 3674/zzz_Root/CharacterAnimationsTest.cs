using System;
using System.Collections.Generic;
using BigAmbitions.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAnimationsTest : MonoBehaviour
{
	[SerializeField]
	private ThirdPersonCharacter tpc;

	[SerializeField]
	private TMP_Dropdown animationsDropdown;

	[SerializeField]
	private TMP_Dropdown permanentAnimationsDropdown;

	[SerializeField]
	private Slider timeSpeedMultiplierSlider;

	private PermanentAnimationType _currentPermanentAnimation;

	private void OnEnable()
	{
		timeSpeedMultiplierSlider.value = 1f;
		Time.timeScale = 1f;
	}

	private void Start()
	{
		SetUpDropdowns();
		timeSpeedMultiplierSlider.onValueChanged.AddListener(SetTimeSpeed);
	}

	private void SetTimeSpeed(float multiplier)
	{
		Time.timeScale = multiplier;
	}

	public void ResetTimeSpeed()
	{
		timeSpeedMultiplierSlider.value = 1f;
	}

	public void ResetAnimations()
	{
		tpc.animator.SetBool(_currentPermanentAnimation, state: false);
		tpc.ResetAnimator();
	}

	private void SetUpDropdowns()
	{
		animationsDropdown.onValueChanged.AddListener(SetAnimation);
		animationsDropdown.options = new List<TMP_Dropdown.OptionData>
		{
			new TMP_Dropdown.OptionData("None")
		};
		foreach (AnimationType value3 in Enum.GetValues(typeof(AnimationType)))
		{
			animationsDropdown.options.Add(new TMP_Dropdown.OptionData(value3.ToStringFast()));
		}
		permanentAnimationsDropdown.onValueChanged.AddListener(SetPermanentAnimation);
		permanentAnimationsDropdown.options = new List<TMP_Dropdown.OptionData>
		{
			new TMP_Dropdown.OptionData("None")
		};
		foreach (PermanentAnimationType value4 in Enum.GetValues(typeof(PermanentAnimationType)))
		{
			permanentAnimationsDropdown.options.Add(new TMP_Dropdown.OptionData(value4.ToStringFast()));
		}
	}

	private void SetAnimation(int index)
	{
		if (index != 0)
		{
			tpc.animator.SetTrigger((AnimationType)(index - 1));
		}
	}

	private void SetPermanentAnimation(int index)
	{
		ResetAnimations();
		if (index != 0)
		{
			_currentPermanentAnimation = (PermanentAnimationType)(index - 1);
			tpc.animator.SetBool(_currentPermanentAnimation);
		}
	}
}
