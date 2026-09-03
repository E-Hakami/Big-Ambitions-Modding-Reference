using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateCachedAvailableProductsForAiCoffeeShopsAndUpdateMarketDemand : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && !(buildingRegistration.businessTypeName != "ba:businesstype_coffeeshop"))
			{
				buildingRegistration.cachedAvailableProducts = buildingRegistration.GetListOfItemsForSale().ToList();
			}
		}
		ProductMarketHelper.FillProvidersDictionary();
		ProductMarketHelper.UpdateMarketDemands();
	}
}
