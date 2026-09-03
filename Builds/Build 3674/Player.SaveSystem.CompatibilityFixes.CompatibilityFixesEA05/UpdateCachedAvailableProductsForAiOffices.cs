using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class UpdateCachedAvailableProductsForAiOffices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && !x.AvailableForRent && x.businessTypeName != "ba:businesstype_empty" && x.BuildingCached.BuildingType == "ba:buildingtype_office"))
		{
			item.cachedAvailableProducts = item.GetListOfItemsForSale().ToList();
		}
	}
}
