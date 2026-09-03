namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class ResetScheduleOnBusinessesBrokenByMovingService : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || !buildingRegistration.BuildingCached)
			{
				continue;
			}
			bool flag = false;
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				foreach (WorkShift workShift in scheduleDay.workShifts)
				{
					if (!buildingRegistration.itemInstances.TryGetValue(workShift.itemInstanceId, out var _))
					{
						buildingRegistration.ResetScheduleDays();
						buildingRegistration.ResetBuildingSpecific();
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
	}
}
