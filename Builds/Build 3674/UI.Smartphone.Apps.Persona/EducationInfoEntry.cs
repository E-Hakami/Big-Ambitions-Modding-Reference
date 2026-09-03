using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.Persona;

public class EducationInfoEntry : MonoBehaviour
{
	private const string CompletedKey = "persona_education_completed";

	private const string NotStartedKey = "persona_education_not_started";

	private const string CompletedPercentageKey = "persona_education_completed_percentage";

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private TextLocalizationComponent statusLabel;

	[SerializeField]
	private GameObject checkmarkObj;

	public void Setup(Diploma diploma)
	{
		headerLabel.Key = diploma.name.GetLocalizeKey();
		if (diploma.completed)
		{
			checkmarkObj.SetActive(value: true);
			statusLabel.Key = "persona_education_completed";
			return;
		}
		checkmarkObj.SetActive(value: false);
		if ((float)diploma.minutesStudied > 0f)
		{
			int requiredMinutes = EducationHelper.GetDiplomaData(diploma.name).requiredMinutes;
			int percentageComplete = Mathf.RoundToInt((float)diploma.minutesStudied / (float)requiredMinutes * 100f);
			statusLabel.Arguments = new { percentageComplete };
			statusLabel.Key = "persona_education_completed_percentage";
		}
		else
		{
			statusLabel.Key = "persona_education_not_started";
		}
	}
}
