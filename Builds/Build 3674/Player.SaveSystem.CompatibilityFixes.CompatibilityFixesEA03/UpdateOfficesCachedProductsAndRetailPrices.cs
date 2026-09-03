using System.Linq;
using AI.Citizens;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class UpdateOfficesCachedProductsAndRetailPrices : ICompatibilityFix
{
	private const string HellsKitchen = "ba:neighborhood_hellskitchen";

	private const string Midtown = "ba:neighborhood_midtown";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
			if (data?.suitableBuildingType == "ba:buildingtype_office")
			{
				buildingRegistration.cachedAvailableProducts = data.GetPrimaryProducts().ToList();
			}
		}
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where(delegate(BuildingRegistration x)
		{
			if (x.RentedByPlayer)
			{
				string businessTypeName = x.businessTypeName;
				return businessTypeName == "ba:businesstype_lawfirm" || businessTypeName == "ba:businesstype_webdevelopmentagency" || businessTypeName == "ba:businesstype_graphicdesigner";
			}
			return false;
		}))
		{
			string neighborhood = item.Neighborhood;
			float num = ((neighborhood == "ba:neighborhood_hellskitchen") ? CitizenHelper.MaxAcceptableRelativePrice(SocialClass.Middle, "ba:neighborhood_hellskitchen") : ((!(neighborhood == "ba:neighborhood_midtown")) ? CitizenHelper.MaxAcceptableRelativePrice(SocialClass.Working, item.Neighborhood) : CitizenHelper.MaxAcceptableRelativePrice(SocialClass.Upper, "ba:neighborhood_midtown")));
			float num2 = num;
			foreach (RetailPrice retailPrice in item.retailPrices)
			{
				float defaultMarketPrice = ItemHelper.GetDefaultMarketPrice(retailPrice.itemName);
				if (defaultMarketPrice != 0f && retailPrice.price / defaultMarketPrice > num2)
				{
					retailPrice.price = num2 * defaultMarketPrice;
				}
			}
		}
	}
}
