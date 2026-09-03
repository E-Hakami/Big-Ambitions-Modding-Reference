using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateCachedAvailableProductsForAiBusinessesAndUpdateMarketDemand : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && (bool)buildingRegistration.BuildingCached && !buildingRegistration.AvailableForRent && buildingRegistration.businessTypeName != "ba:businesstype_empty")
			{
				buildingRegistration.cachedAvailableProducts = buildingRegistration.GetListOfItemsForSale().ToList();
			}
		}
		ProductMarketHelper.FillProvidersDictionary();
		ProductMarketHelper.UpdateMarketDemands(gameInstance);
	}
}
