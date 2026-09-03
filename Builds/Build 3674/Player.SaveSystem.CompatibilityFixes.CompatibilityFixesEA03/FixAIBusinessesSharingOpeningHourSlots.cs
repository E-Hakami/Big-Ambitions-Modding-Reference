using System;
using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixAIBusinessesSharingOpeningHourSlots : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && x.scheduleDays != null))
		{
			foreach (ScheduleDay scheduleDay in item.scheduleDays)
			{
				scheduleDay.openingHourSlots = scheduleDay.openingHourSlots.Copy();
			}
		}
	}
}
