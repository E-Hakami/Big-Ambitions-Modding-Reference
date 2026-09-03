using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Buildings.Schedule;
using Google.OrTools.Sat;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "FreeOnDays", menuName = "BigAmbitions/Employees/JobDemand/FreeOnDays")]
public class FreeOnDays : JobDemand, IScheduleDemand, IScheduleConstraint
{
	[Header("Free On Days Demand")]
	[SerializeField]
	[SearchableEnum]
	private DayOfWeekOrdered[] freeDays;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		for (int i = 0; i < freeDays.Length; i++)
		{
			if (EmployeeWorksOnDay(instance, freeDays[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void ApplyConstraint(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay scheduleDay in scheduler.Registration.scheduleDays)
		{
			if (freeDays.Contains(scheduleDay.day) && scheduler.EmployeeDayDictionary.TryGetValue((scheduleDay.day, employee.id), out var value))
			{
				list.Add(value);
			}
		}
		if (list.Count != 0)
		{
			scheduler.Model.Add(LinearExpr.Sum(list) == 0L);
		}
	}

	private static bool EmployeeWorksOnDay(EmployeeInstance instance, DayOfWeekOrdered day)
	{
		for (int i = 0; i < instance.assignedWeeklyDays.Count; i++)
		{
			if (instance.assignedWeeklyDays[i] == day)
			{
				return true;
			}
		}
		return false;
	}
}
