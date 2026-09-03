using System.Collections.Generic;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Special.MovingCompany;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RunMovingServiceAndAutoScheduleInFactories : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		MovingServiceHelper.itemsSortingMethod = SortByFactoryWorkstations;
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value is FactoryWorkstationInstance)
				{
					MovingServiceHelper.UseMovingServiceInSameBuilding(buildingRegistration);
					AutoFillSchedule(buildingRegistration);
					CompatibilityHelper.KickOutPlayer(gameInstance, buildingRegistration.Address);
					break;
				}
			}
		}
		MovingServiceHelper.itemsSortingMethod = null;
	}

	private static void AutoFillSchedule(BuildingRegistration buildingRegistration)
	{
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = buildingRegistration.Address
		});
		buildingRegistration.AutoFillSchedule(employeeInstances, null, warnIfUnassigned: false);
	}

	private static int SortByFactoryWorkstations(ItemInstance x, ItemInstance y)
	{
		if (x is FactoryWorkstationInstance && y is FactoryWorkstationInstance)
		{
			return 0;
		}
		if (x is FactoryWorkstationInstance)
		{
			return 1;
		}
		if (y is FactoryWorkstationInstance)
		{
			return -1;
		}
		return 0;
	}
}
