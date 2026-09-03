using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RemoveNonExistingDeliveryDrivers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || !(buildingRegistration is Warehouse warehouse))
			{
				continue;
			}
			foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
			{
				if (gameInstance.EmployeeInstances.All((EmployeeInstance x) => x.id != vehicleSlot.employeeDriverId) && gameInstance.CandidateEmployeeInstances.All((EmployeeInstance x) => x.id != vehicleSlot.employeeDriverId))
				{
					vehicleSlot.employeeDriverId = null;
				}
			}
		}
	}
}
