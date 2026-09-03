using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class UpdateCachedAvailableProductsForBrokenAIBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && !x.AvailableForRent && x.businessTypeName != "ba:businesstype_empty" && x.cachedAvailableProducts.Count == 0))
		{
			item.cachedAvailableProducts = item.GetListOfItemsForSale().ToList();
		}
	}
}
