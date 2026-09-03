using BigAmbitions.DayNightCycle;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsNotWorkingDay")]
public class TutorialPointerHideConditionIsNotWorkingDay : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (!BuildingManager.IsInsideBuilding)
		{
			return true;
		}
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (JobInstance jobInstance in SaveGameManager.Current.JobInstances)
		{
			if (jobInstance.address != InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address || !jobInstance.hired)
			{
				continue;
			}
			foreach (ScheduleDay scheduleDay in JobHelper.GetJob(jobInstance.address).scheduleDays)
			{
				if (scheduleDay.day != dayOfWeek)
				{
					continue;
				}
				if (!scheduleDay.isOpen)
				{
					return true;
				}
				foreach (WorkShift workShift in scheduleDay.workShifts)
				{
					if (workShift.startingHour <= SaveGameManager.Current.Hour && workShift.endingHour > SaveGameManager.Current.Hour)
					{
						return false;
					}
				}
				return true;
			}
		}
		return true;
	}
}
