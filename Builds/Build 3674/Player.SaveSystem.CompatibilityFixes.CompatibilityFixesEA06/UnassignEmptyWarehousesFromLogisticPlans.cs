using Buildings.Office.Headquarters;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class UnassignEmptyWarehousesFromLogisticPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && (buildingRegistration.BuildingOwnedByPlayer || buildingRegistration.RentedByPlayer) && (bool)buildingRegistration.BuildingCached && buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse" && buildingRegistration.businessTypeName == "ba:businesstype_empty")
			{
				LogisticsManagerHelper.CancelAllDeliveriesForAddress(buildingRegistration.Address);
			}
		}
	}
}
