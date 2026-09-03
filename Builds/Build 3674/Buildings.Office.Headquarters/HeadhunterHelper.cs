using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using Buildings.BuildingTypes.Office.Headquarters.Headhunter;
using Entities;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public static class HeadhunterHelper
{
	public const string AddressableLabel = "HeadhunterDealBreakers";

	public const int DaysBeforeRetiringNeededForDisplayingEmployee = 7;

	public const float ReplacementFee = 2500f;

	public const int MinSkillTargetValue = 10;

	public const float MaxDealBreakersPoints = 100f;

	public const int DefaultAmountOfCandidatesToRecruit = 10;

	public const float SkillTargetVariation = 5f;

	public static string[] skillsToSkipWhenRecruiting = new string[1] { "ba:skill_negotiation" };

	private static readonly Dictionary<string, List<HeadhunterDealBreakerData>> DealBreakersByCategory = new Dictionary<string, List<HeadhunterDealBreakerData>>();

	public static void OnDealBreakersLoaded(IList<HeadhunterDealBreakerData> dealBreakers)
	{
		DealBreakersByCategory.Clear();
		for (int i = 0; i < dealBreakers.Count; i++)
		{
			HeadhunterDealBreakerData headhunterDealBreakerData = dealBreakers[i];
			if (!DealBreakersByCategory.TryGetValue(headhunterDealBreakerData.category, out var value))
			{
				value = new List<HeadhunterDealBreakerData>(8);
				DealBreakersByCategory.Add(headhunterDealBreakerData.category, value);
			}
			value.Add(headhunterDealBreakerData);
		}
	}

	public static HeadhunterPlan GetAssignedPlanForHeadhunter(string headhunterId)
	{
		return SaveGameManager.Current.headhunterPlans.FirstOrDefault((HeadhunterPlan x) => x.assignedEmployeeId == headhunterId);
	}

	public static HeadhunterPlan GetHeadhunterPlanById(string headhunterPlanId)
	{
		return SaveGameManager.Current.headhunterPlans.FirstOrDefault((HeadhunterPlan x) => x.id == headhunterPlanId);
	}

	public static HeadhunterPlan GetAssignedPlanForHrManagerPlan(string hrManagerPlanId)
	{
		return SaveGameManager.Current.headhunterPlans.FirstOrDefault((HeadhunterPlan x) => x.assignedHrPlans.Contains(hrManagerPlanId));
	}

	public static List<HeadhunterPlan> GetAssignedPlansForHeadquarters(Address headquartersAddress)
	{
		return SaveGameManager.Current.headhunterPlans.Where((HeadhunterPlan x) => x.headquartersAddress == headquartersAddress).ToList();
	}

	public static void DeletePlan(string planId)
	{
		HeadhunterPlan headhunterPlan = SaveGameManager.Current.headhunterPlans.FirstOrDefault((HeadhunterPlan x) => x.id == planId);
		if (headhunterPlan != null)
		{
			headhunterPlan.CancelPendingReplacements();
			SaveGameManager.Current.headhunterPlans.Remove(headhunterPlan);
		}
	}

	public static int CalculateMaxHrPlans(this float skill)
	{
		if (!(skill >= 100f))
		{
			if (skill >= 75f)
			{
				return 1;
			}
			return 0;
		}
		return 2;
	}

	public static int CalculateMaxDealBreakersPoints(this float skill)
	{
		return Mathf.FloorToInt(skill / 100f * 100f);
	}

	public static string[][] GetDealBreakersForSkill(string skillName)
	{
		if (DealBreakersByCategory.Count == 0)
		{
			return Array.Empty<string[]>();
		}
		SkillData data = SkillHelper.GetData(skillName);
		if (data == null || data.possibleDealbreakers == null || data.possibleDealbreakers.Count == 0)
		{
			return Array.Empty<string[]>();
		}
		HashSet<string> hashSet = new HashSet<string>(data.possibleDealbreakers);
		List<string[]> list = new List<string[]>();
		foreach (KeyValuePair<string, List<HeadhunterDealBreakerData>> item in DealBreakersByCategory)
		{
			List<HeadhunterDealBreakerData> value = item.Value;
			if (value == null || value.Count == 0)
			{
				continue;
			}
			List<string> list2 = null;
			for (int i = 0; i < value.Count; i++)
			{
				HeadhunterDealBreakerData headhunterDealBreakerData = value[i];
				if (!(headhunterDealBreakerData == null) && !string.IsNullOrEmpty(headhunterDealBreakerData.type) && hashSet.Contains(headhunterDealBreakerData.type))
				{
					if (list2 == null)
					{
						list2 = new List<string>();
					}
					list2.Add(headhunterDealBreakerData.type);
				}
			}
			if (list2 != null && list2.Count > 0)
			{
				list.Add(list2.ToArray());
			}
		}
		if (list.Count != 0)
		{
			return list.ToArray();
		}
		return Array.Empty<string[]>();
	}

	public static int GetWorkHoursUntilReplacement(ReplacementReason replacementReason)
	{
		Vector2Int daysUntilReplacement = GetDaysUntilReplacement(replacementReason);
		return UnityEngine.Random.Range(daysUntilReplacement.x * 8, daysUntilReplacement.y * 8);
	}

	public static Vector2Int GetDaysUntilReplacement(ReplacementReason replacementReason)
	{
		return replacementReason switch
		{
			ReplacementReason.Satisfaction => new Vector2Int(1, 1), 
			ReplacementReason.Retirement => new Vector2Int(0, 0), 
			ReplacementReason.Poached => new Vector2Int(3, 5), 
			_ => throw new ArgumentOutOfRangeException("replacementReason", replacementReason, null), 
		};
	}

	public static HeadhunterDealBreakerData GetData(string type)
	{
		foreach (List<HeadhunterDealBreakerData> value in DealBreakersByCategory.Values)
		{
			foreach (HeadhunterDealBreakerData item in value)
			{
				if (item.type == type)
				{
					return item;
				}
			}
		}
		return null;
	}
}
