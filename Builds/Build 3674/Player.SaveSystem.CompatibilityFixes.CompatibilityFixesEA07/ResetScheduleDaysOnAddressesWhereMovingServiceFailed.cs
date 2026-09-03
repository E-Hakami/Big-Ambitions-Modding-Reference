using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class ResetScheduleDaysOnAddressesWhereMovingServiceFailed : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (MovingServiceContract movingServiceContract in gameInstance.movingServiceContracts)
		{
			if (movingServiceContract.movingDay < gameInstance.Day || (movingServiceContract.movingDay == gameInstance.Day && movingServiceContract.movingHour < gameInstance.Hour))
			{
				ResetSchedule(BuildingHelper.GetBuildingRegistration(movingServiceContract.originMovingAddress));
				ResetSchedule(BuildingHelper.GetBuildingRegistration(movingServiceContract.destinationMovingAddress));
			}
		}
	}

	private static void ResetSchedule(BuildingRegistration registration)
	{
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = registration.Address
		}))
		{
			employeeInstance.UnAssignWork();
		}
		registration.ResetScheduleDays();
		registration.ResetBuildingSpecific();
	}
}
