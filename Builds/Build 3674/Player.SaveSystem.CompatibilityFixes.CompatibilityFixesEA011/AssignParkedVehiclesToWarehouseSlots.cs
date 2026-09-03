using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public sealed class AssignParkedVehiclesToWarehouseSlots : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration is Warehouse warehouse)
			{
				warehouse.AssignParkedVehiclesToFreeSlots(gameInstance.VehicleInstances);
			}
		}
	}
}
