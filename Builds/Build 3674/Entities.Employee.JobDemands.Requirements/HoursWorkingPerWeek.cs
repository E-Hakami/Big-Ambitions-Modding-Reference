using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Buildings.Schedule;
using Google.OrTools.Sat;
using NaughtyAttributes;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "HoursWorkingPerWeek", menuName = "BigAmbitions/Employees/JobDemand/HoursWorkingPerWeek")]
public class HoursWorkingPerWeek : JobDemand, IScheduleDemand, IScheduleConstraint
{
	[Header("Hours Working Per Week Demand")]
	[SerializeField]
	[MinMaxSlider(0f, 50f)]
	private Vector2Int betweenHours;

	public int MinHours => betweenHours.x;

	public int MaxHours => betweenHours.y;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		if (instance.assignedWeeklyHours >= betweenHours.x && instance.assignedWeeklyHours <= betweenHours.y)
		{
			return instance.workedHoursThisWeek <= betweenHours.y;
		}
		return false;
	}

	public bool IsAboveMax(EmployeeInstance instance)
	{
		return instance.assignedWeeklyHours >= betweenHours.y;
	}

	public void ApplyConstraint(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations)
	{
		ApplyConstraintUsing(scheduler, employee, workStations, betweenHours);
	}

	public static void ApplyConstraintUsing(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations, Vector2Int betweenHours)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay scheduleDay in scheduler.Registration.scheduleDays)
		{
			foreach (WorkStationInfo workStation in workStations)
			{
				foreach (OpeningHourSlot openingHourSlot in scheduleDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						(DayOfWeekOrdered, int, string, string) key = (scheduleDay.day, i, workStation.id, employee.id);
						if (scheduler.ScheduleDictionary.TryGetValue(key, out var value))
						{
							list.Add(value);
						}
					}
				}
			}
		}
		int num = 0;
		if (scheduler.IsSecondPass)
		{
			foreach (var firstPassValue in scheduler.firstPassValues)
			{
				if (firstPassValue.employeeId == employee.id && !HasWorkStationId(workStations, firstPassValue.workstationId))
				{
					num++;
				}
			}
		}
		if (list.Count == 0 || list.Count < betweenHours.x)
		{
			return;
		}
		int num2 = betweenHours.y - num;
		if (scheduler.SkippingSemiCriticalDemands || scheduler.skipOptionalForDayFill || scheduler.IsSecondPass)
		{
			if (num2 >= 0)
			{
				scheduler.Model.Add(LinearExpr.Sum(list) <= num2);
			}
		}
		else if (scheduler.Registration.businessTypeName == "ba:businesstype_headquarters")
		{
			if (num2 >= 0)
			{
				scheduler.Model.AddLinearConstraint(LinearExpr.Sum(list), betweenHours.x, num2);
			}
			else
			{
				scheduler.Model.Add(LinearExpr.Sum(list) >= betweenHours.x);
			}
		}
		else
		{
			scheduler.Model.Add(LinearExpr.Sum(list) == betweenHours.x);
		}
	}

	private static bool HasWorkStationId(List<WorkStationInfo> workStations, string workStationId)
	{
		foreach (WorkStationInfo workStation in workStations)
		{
			if (workStation.id == workStationId)
			{
				return true;
			}
		}
		return false;
	}
}
