using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Persona;

public class CharacterStats : MonoBehaviour
{
	[SerializeField]
	private ProgressBar energyBar;

	[SerializeField]
	private ProgressBar hungerBar;

	[SerializeField]
	private ProgressBar happinessBar;

	[SerializeField]
	private Transform happinessStatsList;

	[SerializeField]
	private Sprite positiveIcon;

	[SerializeField]
	private Sprite negativeIcon;

	private void OnEnable()
	{
		SetUpStats();
	}

	private void SetUpStats()
	{
		energyBar.SetValue(SaveGameManager.Current.Energy, showAnimation: true);
		hungerBar.SetValue(SaveGameManager.Current.Hunger, showAnimation: true);
		happinessBar.SetValue(SaveGameManager.Current.Happiness, showAnimation: true);
		SetUpHappinessModifiers();
	}

	private void SetUpHappinessModifiers()
	{
		if (SaveGameManager.Current.happinessModifiers.Count == 0)
		{
			happinessStatsList.gameObject.SetActive(value: false);
			return;
		}
		Transform transform = happinessStatsList.Find("StatModifierTemplate");
		transform.ResetTemplate();
		foreach (HappinessModifierData happinessModifier in SaveGameManager.Current.happinessModifiers)
		{
			HappinessModifier happinessModifierFromType = HappinessHelper.GetHappinessModifierFromType(happinessModifier.type);
			if (!(happinessModifierFromType == null))
			{
				Transform transform2 = Object.Instantiate(transform, transform.parent);
				transform2.GetLanguageChangeEventByName("Name").Key = happinessModifierFromType.type;
				Color32 color = ((happinessModifierFromType.amount > 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : ((happinessModifierFromType.amount < 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white));
				Image component = transform2.Find("StateIcon").GetComponent<Image>();
				component.sprite = ((happinessModifierFromType.amount >= 0) ? positiveIcon : negativeIcon);
				component.color = color;
				if (happinessModifier.hoursLeft == -1 || happinessModifier.hideDuration)
				{
					transform2.GetLabelByName("TimeLeft").text = "-";
				}
				else
				{
					transform2.GetLanguageChangeEventByName("TimeLeft").SetData((happinessModifier.hoursLeft > 24) ? LanguageChangeEventDataHolder.Create("common_days_left", new
					{
						days = Mathf.RoundToInt((float)happinessModifier.hoursLeft / 24f)
					}) : LanguageChangeEventDataHolder.Create("common_hours_left", new
					{
						hours = happinessModifier.hoursLeft
					}));
				}
				TextMeshProUGUI labelByName = transform2.GetLabelByName("Amount");
				labelByName.text = string.Format("{0}{1}%", (happinessModifierFromType.amount >= 0) ? "+" : "", happinessModifierFromType.amount);
				labelByName.color = color;
				transform2.gameObject.SetActive(value: true);
			}
		}
		happinessStatsList.gameObject.SetActive(value: true);
	}
}
