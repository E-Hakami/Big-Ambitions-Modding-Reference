using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixNightclubFees : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		ProductMarketHelper.UpdateMarketDemands(gameInstance);
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && buildingRegistration.businessTypeName == "ba:businesstype_nightclub")
			{
				buildingRegistration.UpdateCachedAvailableProducts();
			}
		}
	}
}
