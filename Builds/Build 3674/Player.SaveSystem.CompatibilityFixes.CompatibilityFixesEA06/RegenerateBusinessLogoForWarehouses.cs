using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class RegenerateBusinessLogoForWarehouses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (BusinessLogoGenerator.Instance == null)
		{
			return;
		}
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && (bool)buildingRegistration.BuildingCached && buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse" && buildingRegistration.businessTypeName != "ba:businesstype_empty")
			{
				BusinessLogoGenerator.Instance.GenerateWarehouseLogo(buildingRegistration.BusinessName, BusinessTypeHelper.GetData(buildingRegistration), LogoHelper.GetPlayerBusinessLogoPath(buildingRegistration.BusinessName), buildingRegistration.RentedByPlayer);
			}
		}
	}
}
