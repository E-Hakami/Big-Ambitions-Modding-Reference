using BigAmbitions.InteriorDesigner.InteriorElements;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class RemoveCorruptedWorkShiftsAfterAnInstallationContract : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		InteriorElementsHelper.Init();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || !buildingRegistration.BuildingCached)
			{
				continue;
			}
			bool flag = false;
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				for (int num = scheduleDay.workShifts.Count - 1; num >= 0; num--)
				{
					WorkShift workShift = scheduleDay.workShifts[num];
					if (!buildingRegistration.itemInstances.ContainsKey(workShift.itemInstanceId))
					{
						scheduleDay.RemoveWorkShift(workShift);
						flag = true;
					}
				}
			}
			foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = buildingRegistration.Address
			}))
			{
				employeeInstance.UpdateWeeklyHoursAndDays(buildingRegistration.scheduleDays);
				employeeInstance.UpdateAssignedWorkStationItems();
				if (!employeeInstance.IsAssignedToAnyWorkShift())
				{
					employeeInstance.UnAssignWork();
				}
			}
			if (flag)
			{
				CustomerDemandHelper.ReloadCachedFulfilled(buildingRegistration);
				buildingRegistration.UpdateSecurityLevel();
				BusinessHelper.GenerateIdleEmployeesTasks(buildingRegistration);
			}
		}
	}
}
