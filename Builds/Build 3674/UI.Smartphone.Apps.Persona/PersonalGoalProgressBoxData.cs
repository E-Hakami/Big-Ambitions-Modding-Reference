using System;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Persona;

[Serializable]
public class PersonalGoalProgressBoxData
{
	[SerializeField]
	private Image progressBox;

	[SerializeField]
	private BasicTooltip tooltip;

	[SerializeField]
	private Sprite completedSprite;

	[SerializeField]
	private Sprite incompleteSprite;

	[SerializeField]
	private GameObject checkmark;

	public void SetUp(PersonalGoalTierGroup goalTierGroup, int tierIndex)
	{
		bool flag = goalTierGroup.IsTierCompleted(tierIndex);
		progressBox.sprite = (flag ? completedSprite : incompleteSprite);
		checkmark.SetActive(flag);
		SetTooltip(goalTierGroup.GetTier(tierIndex));
	}

	private void SetTooltip(GenericPersonalGoal personalGoal)
	{
		if (!(tooltip == null))
		{
			if (personalGoal == null)
			{
				tooltip.titleKey = "";
				tooltip.localizationArguments = null;
			}
			else
			{
				LanguageChangeEventDataHolder title = personalGoal.GetTitle();
				tooltip.titleKey = title.Key;
				tooltip.localizationArguments = title.Arguments;
			}
		}
	}
}
