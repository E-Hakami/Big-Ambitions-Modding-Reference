using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "MinimumPlayerHappiness", menuName = "BigAmbitions/Employees/JobDemand/MinimumPlayerHappiness")]
public class MinimumPlayerHappiness : JobDemand, IEnvironmentDemand
{
	[Header("Minimum Player Happiness Demand")]
	[SerializeField]
	[Range(0f, 100f)]
	private int minimumPlayerHappiness = 50;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		return HappinessHelper.Happiness >= (float)minimumPlayerHappiness;
	}
}
