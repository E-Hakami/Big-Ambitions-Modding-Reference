using System;
using System.Collections.Generic;
using Buildings.Schedule;
using Google.OrTools.Sat;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "DaysWorkingPerWeek", menuName = "BigAmbitions/Employees/JobDemand/DaysWorkingPerWeek")]
public class DaysWorkingPerWeek : JobDemand, IScheduleDemand, IScheduleConstraint
{
	[Header("Days Working Per Week Demand")]
	[SerializeField]
	private int daysWorkingPerWeek;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		if (instance.assignedWeeklyDays.Count == daysWorkingPerWeek)
		{
			return instance.workedDays <= daysWorkingPerWeek;
		}
		return false;
	}

	public bool IsAboveMax(EmployeeInstance instance)
	{
		return instance.assignedWeeklyDays.Count > daysWorkingPerWeek;
	}

	public void ApplyConstraint(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay scheduleDay in scheduler.Registration.scheduleDays)
		{
			if (scheduler.EmployeeDayDictionary.TryGetValue((scheduleDay.day, employee.id), out var value))
			{
				list.Add(value);
			}
		}
		if (list.Count != 0 && list.Count >= daysWorkingPerWeek)
		{
			scheduler.Model.Add((scheduler.skipOptionalForDayFill || scheduler.IsSecondPass) ? (LinearExpr.Sum(list) <= daysWorkingPerWeek) : (LinearExpr.Sum(list) == daysWorkingPerWeek));
		}
	}
}
