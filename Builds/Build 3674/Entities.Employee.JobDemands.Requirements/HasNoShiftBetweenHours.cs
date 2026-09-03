using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Buildings.Schedule;
using Google.OrTools.Sat;
using NaughtyAttributes;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "HasNoShiftBetweenHours", menuName = "BigAmbitions/Employees/JobDemand/HasNoShiftBetweenHours")]
public class HasNoShiftBetweenHours : JobDemand, IScheduleDemand, IScheduleConstraint
{
	[Header("No Shift Between Hours Demand")]
	[SerializeField]
	[MinMaxSlider(0f, 24f)]
	private Vector2Int shiftPeriod;

	[SerializeField]
	[ShowIf("IsNextShiftPeriodVisible")]
	[MinMaxSlider(0f, 24f)]
	private Vector2Int nextShiftPeriod;

	private bool IsNextShiftPeriodVisible
	{
		get
		{
			if (shiftPeriod.x != 0)
			{
				return shiftPeriod.y == 24;
			}
			return true;
		}
	}

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		bool flag = !instance.HasAnyShiftBetweenHours(shiftPeriod.x, shiftPeriod.y, scheduleDays);
		if (flag && IsNextShiftPeriodVisible)
		{
			flag = !instance.HasAnyShiftBetweenHours(nextShiftPeriod.x, nextShiftPeriod.y, scheduleDays);
		}
		return flag;
	}

	public void ApplyConstraint(ScheduleAutoFiller scheduler, EmployeeInstance employee, List<WorkStationInfo> workStations)
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
						bool flag = i >= shiftPeriod.x && i < shiftPeriod.y;
						if (!flag && IsNextShiftPeriodVisible)
						{
							flag = i >= nextShiftPeriod.x && i < nextShiftPeriod.y;
						}
						if (flag)
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
		}
		if (list.Count != 0)
		{
			scheduler.Model.Add(LinearExpr.Sum(list) == 0L);
		}
	}
}
