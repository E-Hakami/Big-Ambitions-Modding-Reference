using System;
using System.Collections.Generic;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Helpers;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
public class CleanWorkplace : JobDemand, IEnvironmentDemand
{
	[Header("Clean Workplace Demand")]
	[SerializeField]
	[Range(0f, 100f)]
	private int minimumCleanlinessPercentage = 80;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		return CheckIfCleanWorkplace(instance.assignedAddress, minimumCleanlinessPercentage);
	}

	private static bool CheckIfCleanWorkplace(Address assignedAddress, int minimumCleanlinessPercentage)
	{
		if (assignedAddress == null || string.IsNullOrEmpty(assignedAddress.streetName))
		{
			return false;
		}
		return BuildingHelper.GetBuildingRegistration(assignedAddress).GetCleanliness() >= (float)minimumCleanlinessPercentage;
	}
}
