using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class UpdateEmployeeCachedInfo : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			if (employeeInstance.assignedWeeklyDays == null || employeeInstance.assignedWorkStationItems == null)
			{
				employeeInstance.assignedWeeklyDays = new List<DayOfWeekOrdered>();
				employeeInstance.UpdateWeeklyHoursAndDays();
				employeeInstance.assignedWorkStationItems = new List<string>();
				employeeInstance.UpdateAssignedWorkStationItems();
			}
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			if (candidateEmployeeInstance.assignedWeeklyDays == null || candidateEmployeeInstance.assignedWorkStationItems == null)
			{
				candidateEmployeeInstance.assignedWeeklyDays = new List<DayOfWeekOrdered>();
				candidateEmployeeInstance.assignedWorkStationItems = new List<string>();
			}
		}
	}
}
