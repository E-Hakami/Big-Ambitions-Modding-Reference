using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixWorkShiftsWithSoldItems : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		EmployeeHelper.EnsureInit(gameInstance);
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.scheduleDays == null)
			{
				continue;
			}
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				for (int num = scheduleDay.workShifts.Count - 1; num >= 0; num--)
				{
					WorkShift workShift = scheduleDay.workShifts[num];
					if (!buildingRegistration.itemInstances.ContainsKey(workShift.itemInstanceId))
					{
						scheduleDay.RemoveAllWorkShiftsThatMatchPredicate((WorkShift x) => x.itemInstanceId == workShift.itemInstanceId);
					}
				}
			}
		}
	}
}
