using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateCachedAvailableProductsForNightclubs : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && x.businessTypeName == "ba:businesstype_nightclub"))
		{
			item.UpdateCachedAvailableProducts();
		}
		foreach (BuildingRegistration item2 in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && !x.AvailableForRent && x.businessTypeName != "ba:businesstype_empty" && x.BuildingCached.SpecialService == null))
		{
			item2.cachedAvailableProducts = item2.GetListOfItemsForSale().ToList();
		}
	}
}
