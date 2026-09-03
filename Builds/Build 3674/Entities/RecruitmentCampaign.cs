using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Extensions;
using Helpers;
using Localizor;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Entities;

[Serializable]
public class RecruitmentCampaign
{
	[Serializable]
	public class SkillRequirement
	{
		public string skillName;

		public float percentage;
	}

	private const int MaxRandomSkillValue = 50;

	public Address agencyAddress;

	public SkillRequirement skillRequirement;

	public Address businessAddress;

	public int amountOfCandidates;

	public int candidatesFound;

	public float price;

	public bool fullTime;

	public bool partTime;

	public List<Timestamp> candidateFindTimes;

	public bool finished;

	[Obsolete("Can be removed in 0.4")]
	public List<string> candidateEmployeeInstanceIds = new List<string>();

	[Obsolete("Can be removed in 0.4")]
	public bool ageGroup1;

	[Obsolete("Can be removed in 0.4")]
	public bool ageGroup2;

	[Obsolete("Can be removed in 0.4")]
	public bool ageGroup3;

	public void CheckForCandidates()
	{
		int count = candidateFindTimes.Count((Timestamp x) => x.Day == SaveGameManager.Current.Day && x.Hour == SaveGameManager.Current.Hour);
		foreach (int item in Enumerable.Range(0, count))
		{
			_ = item;
			float skillValue = Mathf.Round(UnityEngine.Random.Range(skillRequirement.percentage, 50f));
			RecruitmentHelper.GenerateCandidate(skillRequirement.skillName, skillValue, businessAddress, GetCandidateDemands()).candidateInfo.sourceAddress = agencyAddress;
			candidatesFound++;
		}
		if (candidatesFound >= amountOfCandidates)
		{
			finished = true;
		}
	}

	public void FinishCampaign()
	{
		Contact contact = Contact.GetContact(BuildingHelper.GetBuildingRegistration(agencyAddress), ContactCategoryName.Business);
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				amountOfCandidates.ToString()
			},
			{
				"skillKey",
				skillRequirement.skillName.GetLocalization()
			}
		};
		GameManager.SendTextMessage(contact, "ba:messagetype_phone_recruitment_agency_campaign_finished_info", messageData);
	}

	private List<string> GetCandidateDemands()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (partTime)
		{
			list2.Add("ba:jobdemand_parttime");
		}
		if (fullTime)
		{
			list2.Add("ba:jobdemand_fulltime");
		}
		if (list2.Count > 0)
		{
			list.Add(list2.GetRandom());
		}
		return list;
	}

	public string GetScheduleTypesInfo()
	{
		if (partTime && fullTime)
		{
			return "ba:jobdemand_parttime".GetLocalization() + ", " + "ba:jobdemand_fulltime".GetLocalization();
		}
		if (!partTime)
		{
			return "ba:jobdemand_fulltime".GetLocalization();
		}
		return "ba:jobdemand_parttime".GetLocalization();
	}
}
