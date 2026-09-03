using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes;

public class FixDuplicatedVehicleSlots : ICompatibilityFix
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
				string employeeId = vehicleSlot.employeeDriverId;
				if (!string.IsNullOrEmpty(employeeId))
				{
					EmployeeInstance employeeInstance = gameInstance.EmployeeInstances.FirstOrDefault((EmployeeInstance x) => x.id == employeeId);
					if (employeeInstance == null || employeeInstance.assignedAddress.streetName != buildingRegistration.StreetName || employeeInstance.assignedAddress.streetNumber != buildingRegistration.StreetNumber)
					{
						vehicleSlot.employeeDriverId = null;
					}
				}
				string vehicleId = vehicleSlot.vehicleInstanceId;
				if (!string.IsNullOrEmpty(vehicleId))
				{
					VehicleInstance vehicleInstance = gameInstance.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == vehicleId);
					if (vehicleInstance == null || vehicleInstance.streetName != buildingRegistration.StreetName || vehicleInstance.streetNumber != buildingRegistration.StreetNumber)
					{
						vehicleSlot.vehicleInstanceId = null;
					}
				}
			}
		}
	}
}
