using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

public class HeadhuntersDealBreakers : MonoBehaviour
{
	public Action<string, bool> onDealBreakerToggled;

	[HideInInspector]
	public int availableDealBreakersPoints;

	[HideInInspector]
	public int usedDealBreakersPoints;

	[HideInInspector]
	public List<string> toggledDealBreakersTypes;

	[SerializeField]
	private GameObject noDealBreakersForSelectedSkillWarning;

	[SerializeField]
	private GameObject dealBreakersListPanel;

	[SerializeField]
	private Transform dealBreakerGroupTemplate;

	public void SetDealBreakersForSkill(string skillName, bool enableInteraction)
	{
		dealBreakerGroupTemplate.ResetTemplate();
		usedDealBreakersPoints = 0;
		string[][] dealBreakersForSkill = HeadhunterHelper.GetDealBreakersForSkill(skillName);
		if (dealBreakersForSkill == null)
		{
			noDealBreakersForSelectedSkillWarning.SetActive(value: true);
			dealBreakersListPanel.SetActive(value: false);
		}
		else
		{
			noDealBreakersForSelectedSkillWarning.SetActive(value: false);
			SetUpDealBreakers(dealBreakersForSkill, enableInteraction);
		}
	}

	private void SetUpDealBreakers(string[][] dealBreakers, bool enableInteraction)
	{
		foreach (string[] obj in dealBreakers)
		{
			Transform template = dealBreakerGroupTemplate.CreateElement().Find("DealBreakerTypeTemplate");
			template.ResetTemplate();
			string[] array = obj;
			foreach (string dealBreakerType in array)
			{
				Transform transform = template.CreateElement();
				int rpCost = HeadhunterHelper.GetData(dealBreakerType).recruitmentPointCost;
				transform.GetLanguageChangeEventByName("DealBreakerLabel").SetData("headhunter_deal_breaker_group_label".Localize(new { dealBreakerType, rpCost }));
				Toggle toggle = transform.GetComponent<Toggle>();
				toggle.interactable = enableInteraction;
				bool flag = toggledDealBreakersTypes.Contains(dealBreakerType);
				toggle.SetIsOnWithoutNotify(flag);
				if (enableInteraction)
				{
					toggle.onValueChanged.AddListener(delegate(bool toggled)
					{
						ToggleDealBreakerType(dealBreakerType, toggled, toggle, rpCost);
					});
					if (flag)
					{
						usedDealBreakersPoints += rpCost;
					}
				}
				else if (!flag)
				{
					transform.GetImageByName("Background").SetAlpha(0.5f);
					transform.GetLabelByName("DealBreakerLabel").SetAlpha(0.5f);
				}
			}
		}
		dealBreakersListPanel.SetActive(value: true);
	}

	private void ToggleDealBreakerType(string dealBreakerType, bool toggled, Toggle toggle, int rpCost)
	{
		if (toggled)
		{
			if (usedDealBreakersPoints + rpCost > availableDealBreakersPoints)
			{
				toggle.SetIsOnWithoutNotify(value: false);
				Notifications.ShowError("headhunter_deal_breaker_not_enough_rp");
				return;
			}
			usedDealBreakersPoints += rpCost;
			toggledDealBreakersTypes.Add(dealBreakerType);
		}
		else
		{
			usedDealBreakersPoints -= rpCost;
			toggledDealBreakersTypes.Remove(dealBreakerType);
		}
		onDealBreakerToggled(dealBreakerType, toggled);
	}
}
