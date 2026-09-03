using System.Collections.Generic;
using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixPlayerBusinessSchedulesNotHavingOpeningHourSlots : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where(delegate(BuildingRegistration x)
		{
			if (x.RentedByPlayer)
			{
				string buildingType = x.GetBuildingType();
				return buildingType == "ba:buildingtype_office" || buildingType == "ba:buildingtype_retail";
			}
			return false;
		}))
		{
			foreach (ScheduleDay scheduleDay in item.scheduleDays)
			{
				if (scheduleDay.openingHourSlots == null || scheduleDay.openingHourSlots.Count == 0)
				{
					scheduleDay.openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					};
				}
			}
		}
	}
}
