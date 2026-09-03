using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Dialogs;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UnityEngine;

namespace UI.Dialog;

public class RecruitmentCampaignsList : MonoBehaviour
{
	[SerializeField]
	private Transform contractEntry;

	private void Start()
	{
		Address address = DialogController.current.contact.Address;
		IEnumerable<RecruitmentCampaign> enumerable = SaveGameManager.Current.RecruitmentCampaigns.Where((RecruitmentCampaign x) => x.agencyAddress == address);
		contractEntry.ResetTemplate();
		foreach (RecruitmentCampaign item in enumerable)
		{
			SetUpContract(item);
		}
	}

	private void SetUpContract(RecruitmentCampaign recruitmentCampaign)
	{
		Transform obj = Object.Instantiate(contractEntry, contractEntry.parent);
		string businessName = BuildingHelper.GetBuildingRegistration(recruitmentCampaign.businessAddress).BusinessName;
		int amountOfCandidates = recruitmentCampaign.amountOfCandidates;
		int amountOfCandidatesLeft = recruitmentCampaign.amountOfCandidates - recruitmentCampaign.candidatesFound;
		string skillName = recruitmentCampaign.skillRequirement.skillName;
		int daysLeft = Mathf.Max(1, recruitmentCampaign.candidateFindTimes.OrderBy((Timestamp x) => x.GetTotalMinutes()).Last().Day - SaveGameManager.Current.Day);
		obj.GetLanguageChangeEventByName("Info").SetData("dialog_recruitment_campaigns_list_info".Localize(new
		{
			businessName = businessName,
			skillKey = skillName.GetLocalization(),
			amountOfCandidates = amountOfCandidates,
			scheduleTypes = recruitmentCampaign.GetScheduleTypesInfo(),
			daysLeft = daysLeft,
			amountOfCandidatesLeft = amountOfCandidatesLeft
		}));
		obj.gameObject.SetActive(value: true);
		obj.GetButtonByName("Buttons/CancelCampaignButton").onClick.AddListener(delegate
		{
			((RecruitmentAgencyDialog)DialogController.current.dialog).OnCancelCampaign(recruitmentCampaign).ShowEntry();
		});
	}
}
