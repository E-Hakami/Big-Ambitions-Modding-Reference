using System;
using System.Collections.Generic;
using BigAmbitions.Rivals;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UnityEngine;

namespace AI.Employees;

[Serializable]
public abstract class Complaint
{
	public string complaintMessageType;

	public string complaintMessageNoRivalType;

	public int hoursToHandleComplaint;

	public virtual bool ConditionToComplainMet(EmployeeInstance employeeInstance)
	{
		return true;
	}

	public virtual bool ComplaintHandled(EmployeeInstance employeeInstance)
	{
		return false;
	}

	public virtual (string messageType, Dictionary<string, string> messageData, bool hasRival) GetComplaintMessage(EmployeeInstance employeeInstance)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress);
		string businessBusinessName = GetBusinessBusinessName(buildingRegistration);
		RivalData rivalData = FindSuitableRival(employeeInstance, buildingRegistration);
		Dictionary<string, string> dictionary = new Dictionary<string, string> { { "businessName", businessBusinessName } };
		if (rivalData != null)
		{
			dictionary.Add("rivalName", rivalData.rivalName);
		}
		AddComplaintMessageData(employeeInstance, dictionary);
		return (messageType: (rivalData != null) ? complaintMessageType : complaintMessageNoRivalType, messageData: dictionary, hasRival: rivalData != null);
	}

	protected virtual void AddComplaintMessageData(EmployeeInstance employeeInstance, Dictionary<string, string> messageData)
	{
	}

	protected static string GetBusinessBusinessName(BuildingRegistration business)
	{
		if (business == null)
		{
			return "the_company".GetLocalization();
		}
		return business.BusinessName;
	}

	protected static RivalData FindSuitableRival(EmployeeInstance employeeInstance, BuildingRegistration business)
	{
		string primarySkill = employeeInstance.GetPrimarySkill();
		List<RivalData> allRivalData = RivalsHelper.GetAllRivalData();
		if (business != null)
		{
			RivalData randomSuitableRival = GetRandomSuitableRival(allRivalData, primarySkill, business.Neighborhood);
			if (randomSuitableRival != null)
			{
				return randomSuitableRival;
			}
		}
		return GetRandomSuitableRival(allRivalData, primarySkill);
	}

	private static RivalData GetRandomSuitableRival(List<RivalData> rivals, string employeeSkill)
	{
		RivalData result = null;
		int num = 0;
		for (int i = 0; i < rivals.Count; i++)
		{
			RivalData rivalData = rivals[i];
			if (!RivalsHelper.IsRivalDefeated(rivalData.id) && HasSuitableBusiness(rivalData, employeeSkill))
			{
				num++;
				if (UnityEngine.Random.Range(0, num) == 0)
				{
					result = rivalData;
				}
			}
		}
		return result;
	}

	private static RivalData GetRandomSuitableRival(List<RivalData> rivals, string employeeSkill, string neighborhood)
	{
		RivalData result = null;
		int num = 0;
		for (int i = 0; i < rivals.Count; i++)
		{
			RivalData rivalData = rivals[i];
			if (!RivalsHelper.IsRivalDefeated(rivalData.id) && HasSuitableBusiness(rivalData, employeeSkill, neighborhood))
			{
				num++;
				if (UnityEngine.Random.Range(0, num) == 0)
				{
					result = rivalData;
				}
			}
		}
		return result;
	}

	private static bool HasSuitableBusiness(RivalData rival, string employeeSkill)
	{
		for (int i = 0; i < rival.ownedBusinesses.Count; i++)
		{
			if (IsBuildingRegistrationSuitable(rival.ownedBusinesses[i], employeeSkill))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasSuitableBusiness(RivalData rival, string employeeSkill, string neighborhood)
	{
		for (int i = 0; i < rival.ownedBusinesses.Count; i++)
		{
			if (IsBuildingRegistrationSuitable(rival.ownedBusinesses[i], employeeSkill, neighborhood))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsBuildingRegistrationSuitable(BuildingRegistration registration, string employeeSkill)
	{
		return BuildingContainsEmployeeSkill(registration, employeeSkill);
	}

	private static bool IsBuildingRegistrationSuitable(BuildingRegistration registration, string employeeSkill, string neighborhood)
	{
		if (BuildingContainsEmployeeSkill(registration, employeeSkill))
		{
			return registration.Neighborhood == neighborhood;
		}
		return false;
	}

	private static bool BuildingContainsEmployeeSkill(BuildingRegistration registration, string employeeSkill)
	{
		if (!BuildingTypeHelper.GetData(registration).requiredBuildingSkills.InCollection(employeeSkill))
		{
			return BusinessTypeHelper.GetData(registration).employeePrimarySkills.InCollection(employeeSkill);
		}
		return true;
	}
}
