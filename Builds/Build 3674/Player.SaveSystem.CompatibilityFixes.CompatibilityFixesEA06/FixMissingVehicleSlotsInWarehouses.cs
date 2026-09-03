using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixMissingVehicleSlotsInWarehouses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (Warehouse item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && (bool)x.BuildingCached && x.GetBuildingType() == "ba:buildingtype_warehouse" && ((Warehouse)x).vehicleSlots.Count == 0))
		{
			item.ResetVehicleSlots();
		}
	}
}
