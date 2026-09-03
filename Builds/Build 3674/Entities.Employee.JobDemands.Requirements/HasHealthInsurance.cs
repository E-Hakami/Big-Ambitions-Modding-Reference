using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "HasHealthInsurance", menuName = "BigAmbitions/Employees/JobDemand/HasHealthInsurance")]
public class HasHealthInsurance : JobDemand
{
	[Header("Health Insurance Demand")]
	[SerializeField]
	[SearchableEnum]
	private HealthInsurancePlanType healthInsurancePlan;

	public HealthInsurancePlanType HealthInsurancePlan => healthInsurancePlan;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		return CheckIfHasHealthInsurance(instance, healthInsurancePlan);
	}

	private static bool CheckIfHasHealthInsurance(EmployeeInstance instance, HealthInsurancePlanType planType)
	{
		HrManagerPlan planFromId = HrManagerHelper.GetPlanFromId(instance.assignedHrManagerPlanId);
		if (planFromId?.healthInsurancePlan == null)
		{
			return false;
		}
		if (planFromId.healthInsurancePlan.planType >= planType)
		{
			return planFromId.HasActiveHealthInsurance();
		}
		return false;
	}
}
