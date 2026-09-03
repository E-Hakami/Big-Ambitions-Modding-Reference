namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixWarehousesHavingWrongBusinessTypeName : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && buildingRegistration.BuildingCached.BuildingType == "ba:buildingtype_warehouse")
			{
				buildingRegistration.businessTypeName = "ba:businesstype_warehouse";
			}
		}
	}
}
