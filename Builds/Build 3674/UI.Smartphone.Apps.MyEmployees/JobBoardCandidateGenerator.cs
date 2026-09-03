using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public static class JobBoardCandidateGenerator
{
	private const float MaxCandidateChance = 0.1f;

	private const float PrimarySkillChance = 0.8f;

	private const float MinSkillValue = 5f;

	private const float MaxSkillValue = 40f;

	public static void GenerateJobBoardCandidateIfNeeded(BuildingRegistration registration)
	{
		if (IsRegistrationAbleToGenerateCandidate(registration))
		{
			GenerateJobBoardCandidate(registration);
		}
	}

	private static bool IsRegistrationAbleToGenerateCandidate(BuildingRegistration registration)
	{
		if (!BuildingTypeHelper.GetData(registration).HasTag(TagRef.Buildingtypetag.canjobboardgeneratecandidate))
		{
			return false;
		}
		if (registration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
			if (SaveGameManager.Current.Hour.InRange(8, 16) && dayOfWeek != DayOfWeekOrdered.Saturday && dayOfWeek != DayOfWeekOrdered.Sunday)
			{
				return IsThereAJobBoard(registration);
			}
			return false;
		}
		if (BusinessHelper.IsBusinessOpen(registration))
		{
			return IsThereAJobBoard(registration);
		}
		return false;
	}

	private static bool IsThereAJobBoard(BuildingRegistration registration)
	{
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if (ItemsGetter.GetByName(value.itemName).HasTag(TagRef.Itemtag.isattractingcandidates))
			{
				return true;
			}
		}
		return false;
	}

	private static void GenerateJobBoardCandidate(BuildingRegistration registration)
	{
		float num = (float)registration.promotion.total / 100f * 0.1f;
		if (!(Random.value > num))
		{
			string[] employeePrimarySkills = BusinessTypeHelper.GetData(registration).employeePrimarySkills;
			string[] requiredBuildingSkills = BuildingTypeHelper.GetData(registration).requiredBuildingSkills;
			if ((employeePrimarySkills != null && employeePrimarySkills.Length != 0) || (requiredBuildingSkills != null && requiredBuildingSkills.Length != 0))
			{
				string[] list = ((requiredBuildingSkills != null && requiredBuildingSkills.Length != 0) ? ((Random.value <= 0.8f) ? employeePrimarySkills : requiredBuildingSkills) : employeePrimarySkills);
				string random = list.GetRandom();
				float num2 = Random.Range(5f, 40f);
				EmployeeInstance obj = RecruitmentHelper.GenerateCandidate(secondSkillValue: Random.Range(5f, num2), skillName: random, skillValue: num2, assignedAddress: registration.Address);
				obj.candidateInfo.fromJobBoard = true;
				obj.candidateInfo.sourceAddress = registration.Address;
			}
		}
	}
}
