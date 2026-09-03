using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Persona;

public class PersonalGoalEntry : MonoBehaviour
{
	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TextLocalizationComponent descriptionLabel;

	[SerializeField]
	private TextLocalizationComponent progressLabel;

	[BoxGroup("Progress Boxes")]
	[SerializeField]
	private PersonalGoalProgressBoxData bronzeProgressBox = new PersonalGoalProgressBoxData();

	[BoxGroup("Progress Boxes")]
	[SerializeField]
	private PersonalGoalProgressBoxData silverProgressBox = new PersonalGoalProgressBoxData();

	[BoxGroup("Progress Boxes")]
	[SerializeField]
	private PersonalGoalProgressBoxData goldProgressBox = new PersonalGoalProgressBoxData();

	[BoxGroup("Medal")]
	[SerializeField]
	private Image medalImage;

	[BoxGroup("Medal")]
	[SerializeField]
	private Sprite bronzeMedal;

	[BoxGroup("Medal")]
	[SerializeField]
	private Sprite silverMedal;

	[BoxGroup("Medal")]
	[SerializeField]
	private Sprite goldMedal;

	public void Setup(PersonalGoalTierGroup goalTierGroup)
	{
		if (goalTierGroup == null)
		{
			return;
		}
		GenericPersonalGoal displayGoal = goalTierGroup.DisplayGoal;
		if (displayGoal == null)
		{
			return;
		}
		if (displayGoal.icon != null)
		{
			iconImage.sprite = displayGoal.icon;
		}
		if (descriptionLabel != null)
		{
			descriptionLabel.SetData(displayGoal.GetTitle());
		}
		if (progressLabel != null)
		{
			bool flag = displayGoal.HasProgress();
			progressLabel.gameObject.SetActive(flag);
			if (flag)
			{
				progressLabel.SetData(displayGoal.GetProgress());
			}
		}
		bronzeProgressBox.SetUp(goalTierGroup, 0);
		silverProgressBox.SetUp(goalTierGroup, 1);
		goldProgressBox.SetUp(goalTierGroup, 2);
		SetSteamMedal(goalTierGroup.HighestCompletedSteamTierIndex);
	}

	private void SetSteamMedal(int tierIndex)
	{
		Sprite steamMedal = GetSteamMedal(tierIndex);
		bool flag = steamMedal != null;
		medalImage.gameObject.SetActive(flag);
		if (flag)
		{
			medalImage.sprite = steamMedal;
		}
	}

	private Sprite GetSteamMedal(int tierIndex)
	{
		return tierIndex switch
		{
			0 => bronzeMedal, 
			1 => silverMedal, 
			2 => goldMedal, 
			_ => null, 
		};
	}
}
