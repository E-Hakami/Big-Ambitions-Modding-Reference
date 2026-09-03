using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Entities;
using Helpers;

[Serializable]
public class ScheduleDay
{
	public DayOfWeekOrdered day;

	public bool isOpen;

	public List<OpeningHourSlot> openingHourSlots = new List<OpeningHourSlot>();

	public List<WorkShift> workShifts = new List<WorkShift>();

	public int GetHoursOpen => openingHourSlots.Sum((OpeningHourSlot x) => x.GetDurationInHours);

	public void AddWorkShift(WorkShift workShift)
	{
		workShifts.Add(workShift);
	}

	public void RemoveWorkShift(WorkShift workShift)
	{
		WorkShift workShift2 = ((workShifts.IndexOf(workShift) == -1) ? workShifts.FirstOrDefault((WorkShift x) => x.startingHour == workShift.startingHour && x.endingHour == workShift.endingHour && x.employeeId == workShift.employeeId) : workShift);
		if (workShift2 != null)
		{
			workShifts.Remove(workShift2);
		}
	}

	public void ClearWorkShifts()
	{
		RemoveAllWorkShiftsThatMatchPredicate(null);
	}

	public void RemoveAllWorkShiftsThatMatchPredicate(Predicate<WorkShift> match)
	{
		for (int num = workShifts.Count - 1; num >= 0; num--)
		{
			if (match == null || match(workShifts[num]))
			{
				if (!string.IsNullOrEmpty(workShifts[num].employeeId))
				{
					EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(workShifts[num].employeeId);
					workShifts[num].employeeId = null;
					if (employeeById != null)
					{
						employeeById.UpdateWeeklyHoursAndDays();
						employeeById.UpdateAssignedWorkStationItems();
					}
				}
				RemoveWorkShift(workShifts[num]);
			}
		}
	}

	public bool IsOpenAtHour(int hour)
	{
		if (!isOpen)
		{
			return false;
		}
		foreach (OpeningHourSlot openingHourSlot in openingHourSlots)
		{
			if (openingHourSlot.startingHour <= hour && openingHourSlot.endingHour > hour)
			{
				return true;
			}
		}
		return false;
	}

	public (int, int) GetNextContinuousOpenHours(int minHour)
	{
		int num = -1;
		int num2 = -1;
		bool flag = false;
		for (int i = minHour; i < 24; i++)
		{
			if (IsOpenAtHour(i))
			{
				if (!flag)
				{
					num = i;
					flag = true;
				}
			}
			else if (flag)
			{
				num2 = i;
				break;
			}
		}
		if (num == -1)
		{
			return (-1, -1);
		}
		if (num2 == -1)
		{
			num2 = 24;
		}
		return (num, num2);
	}
}
