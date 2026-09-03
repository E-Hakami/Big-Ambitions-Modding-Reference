using System.Collections.Generic;
using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixBusinessesTakenOverSharingSameSchedule : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer))
		{
			List<ScheduleDay> scheduleDays = item.scheduleDays;
			item.scheduleDays = new List<ScheduleDay>();
			foreach (ScheduleDay item2 in scheduleDays)
			{
				ScheduleDay scheduleDay = new ScheduleDay
				{
					day = item2.day,
					isOpen = item2.isOpen
				};
				foreach (OpeningHourSlot openingHourSlot in item2.openingHourSlots)
				{
					scheduleDay.openingHourSlots.Add(openingHourSlot.Clone());
				}
				foreach (WorkShift workShift in item2.workShifts)
				{
					scheduleDay.workShifts.Add(workShift.Clone());
				}
				item.scheduleDays.Add(scheduleDay);
			}
		}
	}
}
