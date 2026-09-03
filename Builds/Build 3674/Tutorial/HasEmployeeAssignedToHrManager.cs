using System.Linq;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasEmployeeAssignedToHrManager")]
public class HasEmployeeAssignedToHrManager : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}).Any((EmployeeInstance x) => !string.IsNullOrEmpty(x.assignedHrManagerPlanId));
	}
}
