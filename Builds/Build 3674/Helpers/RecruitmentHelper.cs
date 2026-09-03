using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Skills;
using Entities;
using Extensions;
using JimmysUnityUtilities;
using UI;
using UnityEngine;

namespace Helpers;

public static class RecruitmentHelper
{
	private const float MinSecondaryInitialSkill = 5f;

	private const float MaxSecondaryInitialSkill = 20f;

	private const int MinEmployeeAge = 18;

	private const int MaxEmployeeAge = 65;

	private static HashSet<string> CachedExistingEmployeeNames;

	public static float GetRandomSecondarySkillInitialValue => Mathf.Round(UnityEngine.Random.Range(5f, 20f));

	public static void RunHourly()
	{
		foreach (RecruitmentCampaign recruitmentCampaign in SaveGameManager.Current.RecruitmentCampaigns)
		{
			recruitmentCampaign.CheckForCandidates();
		}
		SaveGameManager.Current.RecruitmentCampaigns.Where((RecruitmentCampaign x) => x.finished).ForEach(delegate(RecruitmentCampaign x)
		{
			x.FinishCampaign();
		});
		SaveGameManager.Current.RecruitmentCampaigns.RemoveAll((RecruitmentCampaign x) => x.finished);
	}

	public static EmployeeInstance GenerateCandidate(string skillName, float skillValue, Address assignedAddress, List<string> demands = null, float secondSkillValue = 0f)
	{
		EmployeeInstance employeeInstance = skillName switch
		{
			"ba:skill_logisticsmanager" => new LogisticsManager(), 
			"ba:skill_hrmanager" => new HRManager(), 
			"ba:skill_headhunter" => new Headhunter(), 
			"ba:skill_pricingmanager" => new PricingManager(), 
			_ => new EmployeeInstance(), 
		};
		employeeInstance.Initialize();
		var (gender, name) = GetRandomGenderAndName();
		employeeInstance.characterData.gender = gender;
		employeeInstance.characterData.name = name;
		employeeInstance.candidateInfo = new CandidateInfo
		{
			hoursUntilExpiring = 168
		};
		employeeInstance.assignedAddress = assignedAddress;
		employeeInstance.characterData.skills.Add(new Skill
		{
			name = skillName,
			value = skillValue
		});
		SkillData data = SkillHelper.GetData(skillName);
		if (!string.IsNullOrEmpty(data.secondarySkill))
		{
			employeeInstance.characterData.skills.Add(new Skill
			{
				name = data.secondarySkill,
				value = ((secondSkillValue > 0f) ? secondSkillValue : GetRandomSecondarySkillInitialValue)
			});
		}
		employeeInstance.demands = demands ?? new List<string>();
		employeeInstance.GenerateDemands();
		employeeInstance.satisfaction = ((employeeInstance.demands.Count == 0) ? 100 : 50);
		employeeInstance.characterData.ageInDays = GetRandomEmployeeAgeInDays();
		employeeInstance.hourlyWage = EmployeeHelper.CalculateHourlyWageForSkill(employeeInstance.characterData.skills.First());
		employeeInstance.initialCombinedSkillAmount = employeeInstance.characterData.skills.Sum((Skill x) => x.value);
		SaveGameManager.Current.CandidateEmployeeInstances.Add(employeeInstance);
		EmployeeHelper.EmployeeInstancesDictionary.Add(employeeInstance.id, employeeInstance);
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.MyEmployees, playSound: false);
		GameEvent.Invoke("ba:gameevent_candidatereceived");
		return employeeInstance;
	}

	public static (Gender, string) GetRandomGenderAndName()
	{
		if (CachedExistingEmployeeNames == null)
		{
			CachedExistingEmployeeNames = GetExistingEmployeeNames();
		}
		Gender randomEnum = RngHelper.GetRandomEnum<Gender>();
		string randomNameForGender = CharacterNames.GetRandomNameForGender(randomEnum);
		while (CachedExistingEmployeeNames.Contains(randomNameForGender))
		{
			randomNameForGender = CharacterNames.GetRandomNameForGender(randomEnum);
		}
		CachedExistingEmployeeNames.Add(randomNameForGender);
		return (randomEnum, randomNameForGender);
	}

	private static HashSet<string> GetExistingEmployeeNames()
	{
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances();
		HashSet<string> hashSet = new HashSet<string>(employeeInstances.Count);
		foreach (EmployeeInstance item in employeeInstances)
		{
			if (item.characterData != null)
			{
				hashSet.Add(item.characterData.name);
			}
		}
		return hashSet;
	}

	public static int GetRandomEmployeeAgeInDays()
	{
		return TimeHelper.GetDaysByYears(UnityEngine.Random.Range(18, 65));
	}

	public static int GetRandomEmployeeAgeInDays(System.Random rand)
	{
		return TimeHelper.GetDaysByYears(rand.Next(18, 65));
	}
}
