using System.Linq;
using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixNullEmployeeInWorkShifts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		int num = 0;
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer))
		{
			foreach (ScheduleDay scheduleDay in item.scheduleDays)
			{
				num += scheduleDay.workShifts.RemoveAll((WorkShift x) => x.employeeId == null || gameInstance.EmployeeInstances.All((EmployeeInstance y) => y.id != x.employeeId));
			}
		}
		if (num > 0)
		{
			Debug.LogWarning($"Null employees found in work shifts and removed: {num}");
		}
	}
}
