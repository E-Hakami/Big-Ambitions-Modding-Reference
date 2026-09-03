using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public static class HrManagerHelper
{
	public const int PrimarySkillTrainingPerDay = 2;

	public const int SecondarySkillTrainingPerDay = 1;

	public static HrManagerPlan GetAssignedPlanForHrManager(string hrManagerId)
	{
		return SaveGameManager.Current.hrManagerPlans.FirstOrDefault((HrManagerPlan x) => x.assignedEmployeeId == hrManagerId);
	}

	public static HrManagerPlan GetPlanFromId(string planId)
	{
		for (int i = 0; i < SaveGameManager.Current.hrManagerPlans.Count; i++)
		{
			if (SaveGameManager.Current.hrManagerPlans[i].id == planId)
			{
				return SaveGameManager.Current.hrManagerPlans[i];
			}
		}
		return null;
	}

	public static List<HrManagerPlan> GetAssignedPlansForHeadquarters(Address headquartersAddress)
	{
		return SaveGameManager.Current.hrManagerPlans.Where((HrManagerPlan x) => x.headquartersAddress == headquartersAddress).ToList();
	}

	public static void DeletePlan(string planId)
	{
		SaveGameManager.Current.hrManagerPlans.FirstOrDefault((HrManagerPlan x) => x.id == planId)?.Delete();
	}

	public static int CalculateMaxAssignableEmployees(this float skill)
	{
		return 10 + Mathf.FloorToInt(skill / 5f / 5f) * 10;
	}
}
